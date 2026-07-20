using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using GitSubmoduleSync.Models;

namespace GitSubmoduleSync.Services;

public sealed class DependencyGraphService
{
  private static readonly XNamespace Ns = "http://schemas.microsoft.com/developer/msbuild/2003";
  private static readonly string[] PastasExcluidas = { "bin", "obj", "packages", ".git", "node_modules" };
  private static readonly Regex RegexProjetoDeTeste = new(@"\.(TesteUnitario|TestesUnitarios|TesteUnitarios)$", RegexOptions.Compiled);

  private static readonly JsonSerializerOptions JsonOpcoes = new() { WriteIndented = false };

  public Grafo Montar(SyncProfile perfil, IProgress<LogEvent>? log = null)
  {
    var csprojPaths = DescobrirCsproj(perfil.PastaRaiz);
    log?.Report(new LogEvent(NivelLog.Detalhe, $"{csprojPaths.Count} arquivos .csproj descobertos."));

    var chaveCache = CalcularHashCache(csprojPaths, perfil.IgnorarProjetosDeTeste, perfil.SubmodulosIgnorados);
    var cacheado = TentarCarregarCache(perfil.PastaRaiz, chaveCache);
    if (cacheado is not null)
    {
      log?.Report(new LogEvent(NivelLog.Detalhe, "Grafo reaproveitado do cache."));
      return cacheado;
    }

    var todosOsNos = csprojPaths.Select(p => ParsearCsproj(p, perfil.PastaRaiz)).ToList();

    var nos = perfil.IgnorarProjetosDeTeste
      ? todosOsNos.Where(n => !n.EhProjetoDeTeste).ToList()
      : todosOsNos;

    if (perfil.SubmodulosIgnorados.Count > 0)
    {
      nos = nos.Where(n => !perfil.SubmodulosIgnorados.Contains(n.Submodulo, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    var porCaminho = nos.ToDictionary(n => n.CaminhoCsproj, n => n, StringComparer.OrdinalIgnoreCase);
    var porAssembly = new Dictionary<string, NoInterno>(StringComparer.Ordinal);
    foreach (var n in nos)
    {
      porAssembly.TryAdd(n.AssemblyName, n);
    }

    var avisos = new List<string>();
    var arestas = new Dictionary<string, List<string>>(StringComparer.Ordinal);

    foreach (var n in nos)
    {
      var deps = new HashSet<string>(StringComparer.Ordinal);

      foreach (var caminhoRef in n.ProjectReferences)
      {
        if (porCaminho.TryGetValue(caminhoRef, out var alvo))
        {
          deps.Add(alvo.AssemblyName);
        }
      }

      arestas[n.AssemblyName] = deps.ToList();
    }

    // Segunda passada: arestas via HintPath resolvido para Bin\Custom (a referência binária entre submódulos)
    foreach (var n in nos)
    {
      foreach (var custom in n.RefsCustomInternas)
      {
        var nomeAlvo = Path.GetFileNameWithoutExtension(custom);
        if (porAssembly.TryGetValue(nomeAlvo, out var alvo))
        {
          if (!string.Equals(alvo.AssemblyName, n.AssemblyName, StringComparison.Ordinal))
          {
            arestas[n.AssemblyName].Add(alvo.AssemblyName);
          }
        }
        else
        {
          avisos.Add($"[{n.AssemblyName}] HintPath via Bin\\Custom sem projeto correspondente: {nomeAlvo}");
        }
      }
      arestas[n.AssemblyName] = arestas[n.AssemblyName].Distinct(StringComparer.Ordinal).ToList();
    }

    var (ondas, ciclos) = OrdenarEmOndas(arestas, porAssembly);

    var binCustomResolvido = ResolverBinCustom(nos);

    foreach (var n in nos.Where(n => !n.EhProjetoDeTeste))
    {
      if (!string.Equals(n.OutputPathAbsoluto, binCustomResolvido, StringComparison.OrdinalIgnoreCase))
      {
        avisos.Add($"[{n.AssemblyName}] OutputPath diverge do Bin\\Custom resolvido ('{n.OutputPathAbsoluto}' != '{binCustomResolvido}').");
      }
    }

    if (ciclos.Count > 0)
    {
      avisos.Add($"Ciclo detectado envolvendo {ciclos.Count} projeto(s): {string.Join(", ", ciclos.Take(10))}{(ciclos.Count > 10 ? "…" : "")}");
    }

    var grafo = new Grafo(
      Nos: nos.Select(n => n.ParaProjectNode()).ToList(),
      Ondas: ondas,
      Arestas: arestas.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value),
      Ciclos: ciclos,
      Avisos: avisos,
      BinCustomResolvido: binCustomResolvido);

    SalvarCache(perfil.PastaRaiz, chaveCache, grafo);

    return grafo;
  }

  // --- Descoberta ---

  private static List<string> DescobrirCsproj(string pastaRaiz)
  {
    // Poda a travessia nas pastas excluídas em vez de filtrar depois: descer para dentro de
    // node_modules (o dataprev_PortalMeuRH_web tem uma dependência C++ do lmdb com milhares de
    // arquivos) sem necessidade é o que torna a descoberta lenta.
    var resultado = new List<string>();
    CaminharDiretorio(pastaRaiz, resultado);
    return resultado;
  }

  private static void CaminharDiretorio(string diretorio, List<string> resultado)
  {
    IEnumerable<string> subpastas;
    try
    {
      resultado.AddRange(Directory.EnumerateFiles(diretorio, "*.csproj"));
      subpastas = Directory.EnumerateDirectories(diretorio);
    }
    catch (UnauthorizedAccessException)
    {
      return;
    }

    foreach (var sub in subpastas)
    {
      if (PastasExcluidas.Contains(Path.GetFileName(sub), StringComparer.OrdinalIgnoreCase))
      {
        continue;
      }
      CaminharDiretorio(sub, resultado);
    }
  }

  // --- Parse ---

  private sealed record NoInterno(
    string CaminhoCsproj,
    string Submodulo,
    string AssemblyName,
    string OutputPathAbsoluto,
    bool EhProjetoDeTeste,
    List<string> ProjectReferences,
    List<RefExterna> ReferenciasExternas,
    List<string> RefsCustomInternas)
  {
    public ProjectNode ParaProjectNode() => new(
      CaminhoCsproj, Submodulo, AssemblyName, OutputPathAbsoluto, EhProjetoDeTeste,
      ProjectReferences, ReferenciasExternas);
  }

  private static NoInterno ParsearCsproj(string caminhoCsproj, string pastaRaiz)
  {
    var xml = XDocument.Load(caminhoCsproj);
    var dir = Path.GetDirectoryName(caminhoCsproj)!;

    var assemblyName = xml.Descendants(Ns + "AssemblyName").FirstOrDefault()?.Value.Trim();
    if (string.IsNullOrWhiteSpace(assemblyName))
    {
      assemblyName = Path.GetFileNameWithoutExtension(caminhoCsproj);
    }

    var outputPath = "";
    foreach (var pg in xml.Descendants(Ns + "PropertyGroup"))
    {
      var condicao = pg.Attribute("Condition")?.Value ?? "";
      if (condicao.Contains("Debug", StringComparison.Ordinal))
      {
        var op = pg.Element(Ns + "OutputPath")?.Value;
        if (!string.IsNullOrWhiteSpace(op))
        {
          outputPath = op;
        }
      }
    }
    var outputPathAbsoluto = string.IsNullOrEmpty(outputPath)
      ? ""
      : Path.GetFullPath(Path.Combine(dir, outputPath)).TrimEnd(Path.DirectorySeparatorChar);

    var submodulo = ObterSubmodulo(caminhoCsproj, pastaRaiz);

    var projectReferences = xml.Descendants(Ns + "ProjectReference")
      .Select(pr => pr.Attribute("Include")?.Value)
      .Where(inc => !string.IsNullOrWhiteSpace(inc))
      .Select(inc => Path.GetFullPath(Path.Combine(dir, inc!)))
      .ToList();

    var referenciasExternas = new List<RefExterna>();
    var refsCustomInternas = new List<string>();

    foreach (var hintPath in xml.Descendants(Ns + "Reference").Select(r => r.Element(Ns + "HintPath")?.Value))
    {
      if (string.IsNullOrWhiteSpace(hintPath)) continue;

      string abs;
      try { abs = Path.GetFullPath(Path.Combine(dir, hintPath)); }
      catch { continue; }

      var nome = Path.GetFileNameWithoutExtension(abs);

      if (abs.Contains(@"\Bin\Custom\", StringComparison.OrdinalIgnoreCase))
      {
        refsCustomInternas.Add(abs);
      }
      else if (abs.Contains(@"\packages\", StringComparison.OrdinalIgnoreCase))
      {
        referenciasExternas.Add(new RefExterna(nome, abs, TipoRefExterna.PacoteNuGet));
      }
      else if (abs.Contains(@"\Bin\", StringComparison.OrdinalIgnoreCase))
      {
        referenciasExternas.Add(new RefExterna(nome, abs, TipoRefExterna.BinProdutoRM));
      }
    }

    var ehTeste = RegexProjetoDeTeste.IsMatch(assemblyName!);

    return new NoInterno(
      caminhoCsproj, submodulo, assemblyName!, outputPathAbsoluto, ehTeste,
      projectReferences, referenciasExternas, refsCustomInternas);
  }

  private static string ObterSubmodulo(string caminhoCsproj, string pastaRaiz)
  {
    var relativo = Path.GetRelativePath(pastaRaiz, caminhoCsproj);
    var primeiraParte = relativo.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    return primeiraParte ?? "";
  }

  // --- Ondas (Kahn) ---

  private static (List<IReadOnlyList<ProjectNode>> Ondas, List<string> Ciclos) OrdenarEmOndas(
    Dictionary<string, List<string>> arestas, Dictionary<string, NoInterno> porAssembly)
  {
    var restantes = arestas.ToDictionary(kv => kv.Key, kv => new List<string>(kv.Value), StringComparer.Ordinal);
    var ondas = new List<IReadOnlyList<ProjectNode>>();

    while (restantes.Count > 0)
    {
      var ondaChaves = restantes.Keys
        .Where(k => restantes[k].All(dep => !restantes.ContainsKey(dep)))
        .OrderBy(k => k, StringComparer.Ordinal)
        .ToList();

      if (ondaChaves.Count == 0)
      {
        break; // ciclo: sobra em restantes
      }

      foreach (var k in ondaChaves)
      {
        restantes.Remove(k);
      }

      ondas.Add(ondaChaves.Select(k => porAssembly[k].ParaProjectNode()).ToList());
    }

    var ciclos = restantes.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
    return (ondas, ciclos);
  }

  // --- Bin\Custom resolvido (moda dos caminhos absolutos) ---

  private static string ResolverBinCustom(List<NoInterno> nos)
  {
    return nos
      .Where(n => !n.EhProjetoDeTeste && !string.IsNullOrEmpty(n.OutputPathAbsoluto))
      .GroupBy(n => n.OutputPathAbsoluto, StringComparer.OrdinalIgnoreCase)
      .OrderByDescending(g => g.Count())
      .Select(g => g.Key)
      .FirstOrDefault() ?? "";
  }

  // --- Cache ---

  private static string CaminhoCache(string pastaRaiz) => Path.Combine(pastaRaiz, ".gss-cache", "graph.json");

  private sealed record GrafoCache(string Hash, Grafo Grafo);

  private static string CalcularHashCache(List<string> csprojPaths, bool ignorarProjetosDeTeste, List<string> submodulosIgnorados)
  {
    var entradas = csprojPaths
      .Select(p => $"{p.ToLowerInvariant()}|{File.GetLastWriteTimeUtc(p).Ticks}")
      .OrderBy(s => s, StringComparer.Ordinal)
      .ToList();

    entradas.Add($"IgnorarProjetosDeTeste={ignorarProjetosDeTeste}");
    entradas.Add($"SubmodulosIgnorados={string.Join(",", submodulosIgnorados.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))}");

    var bytes = Encoding.UTF8.GetBytes(string.Join("\n", entradas));
    return Convert.ToHexString(SHA256.HashData(bytes));
  }

  private static Grafo? TentarCarregarCache(string pastaRaiz, string chaveCache)
  {
    var caminho = CaminhoCache(pastaRaiz);
    if (!File.Exists(caminho)) return null;

    try
    {
      var json = File.ReadAllText(caminho);
      var cache = JsonSerializer.Deserialize<GrafoCache>(json, JsonOpcoes);
      return cache is not null && cache.Hash == chaveCache ? cache.Grafo : null;
    }
    catch (JsonException)
    {
      return null;
    }
  }

  private static void SalvarCache(string pastaRaiz, string chaveCache, Grafo grafo)
  {
    try
    {
      var caminho = CaminhoCache(pastaRaiz);
      Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);

      var json = JsonSerializer.Serialize(new GrafoCache(chaveCache, grafo), JsonOpcoes);
      var tmp = caminho + ".tmp";
      File.WriteAllText(tmp, json);
      File.Move(tmp, caminho, overwrite: true);
    }
    catch (IOException)
    {
      // cache é uma otimização, não um requisito — falha ao gravar não deve interromper a execução
    }
    catch (UnauthorizedAccessException)
    {
    }
  }
}
