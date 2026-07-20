using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GitSubmoduleSync.Services;

/// <summary>
/// Regeneração opcional do BuildAll.sln a partir do grafo real — ação separada no menu,
/// nunca automática. Diferente do .bat, as dependências de projeto vêm das arestas reais
/// do grafo, não de um heurística por nome.
/// </summary>
public sealed class BuildAllSlnService
{
  private static readonly XNamespace Ns = "http://schemas.microsoft.com/developer/msbuild/2003";
  private const string TipoProjetoCSharp = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";

  private static readonly Regex RegexProjeto = new(
    "^Project\\(\"\\{[^}]+\\}\"\\)\\s*=\\s*\"(?<nome>[^\"]+)\",\\s*\"[^\"]+\",\\s*\"(?<guid>\\{[^}]+\\})\"",
    RegexOptions.Compiled);

  public void Regenerar(Grafo grafo, string pastaRaiz)
  {
    var caminhoSln = Path.Combine(pastaRaiz, "BuildAll.sln");
    var guidsExistentes = LerGuidsExistentes(caminhoSln);

    var guidPorAssembly = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var no in grafo.Nos)
    {
      guidPorAssembly[no.AssemblyName] = ResolverGuid(no, guidsExistentes);
    }

    var sb = new StringBuilder();
    sb.AppendLine();
    sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
    sb.AppendLine("# Visual Studio Version 17");

    foreach (var no in grafo.Nos)
    {
      var guid = guidPorAssembly[no.AssemblyName];
      var caminhoRelativo = Path.GetRelativePath(pastaRaiz, no.CaminhoCsproj);
      sb.AppendLine($"Project(\"{TipoProjetoCSharp}\") = \"{no.AssemblyName}\", \"{caminhoRelativo}\", \"{guid}\"");

      if (grafo.Arestas.TryGetValue(no.AssemblyName, out var deps) && deps.Count > 0)
      {
        sb.AppendLine("\tProjectSection(ProjectDependencies) = postProject");
        foreach (var dep in deps)
        {
          if (guidPorAssembly.TryGetValue(dep, out var depGuid))
          {
            sb.AppendLine($"\t\t{depGuid} = {depGuid}");
          }
        }
        sb.AppendLine("\tEndProjectSection");
      }
      sb.AppendLine("EndProject");
    }

    sb.AppendLine("Global");
    sb.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
    sb.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
    sb.AppendLine("\tEndGlobalSection");
    sb.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
    foreach (var no in grafo.Nos)
    {
      var guid = guidPorAssembly[no.AssemblyName];
      sb.AppendLine($"\t\t{guid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
      sb.AppendLine($"\t\t{guid}.Debug|Any CPU.Build.0 = Debug|Any CPU");
    }
    sb.AppendLine("\tEndGlobalSection");
    sb.AppendLine("\tGlobalSection(SolutionProperties) = preSolution");
    sb.AppendLine("\t\tHideSolutionNode = FALSE");
    sb.AppendLine("\tEndGlobalSection");
    sb.AppendLine("EndGlobal");

    var tmp = caminhoSln + ".tmp";
    File.WriteAllText(tmp, sb.ToString());
    File.Move(tmp, caminhoSln, overwrite: true);
  }

  private static string ResolverGuid(ProjectNode no, Dictionary<string, string> guidsExistentes)
  {
    var doCsproj = LerProjectGuidDoCsproj(no.CaminhoCsproj);
    if (doCsproj is not null) return doCsproj;

    if (guidsExistentes.TryGetValue(no.AssemblyName, out var existente)) return existente;

    return "{" + Guid.NewGuid().ToString().ToUpperInvariant() + "}";
  }

  private static string? LerProjectGuidDoCsproj(string caminhoCsproj)
  {
    try
    {
      var xml = XDocument.Load(caminhoCsproj);
      var valor = xml.Descendants(Ns + "ProjectGuid").FirstOrDefault()?.Value.Trim();
      return string.IsNullOrWhiteSpace(valor) ? null : valor.ToUpperInvariant();
    }
    catch (Exception ex) when (ex is IOException or System.Xml.XmlException)
    {
      return null;
    }
  }

  private static Dictionary<string, string> LerGuidsExistentes(string caminhoSln)
  {
    var resultado = new Dictionary<string, string>(StringComparer.Ordinal);
    if (!File.Exists(caminhoSln)) return resultado;

    foreach (var linha in File.ReadAllLines(caminhoSln))
    {
      var m = RegexProjeto.Match(linha);
      if (m.Success)
      {
        resultado[m.Groups["nome"].Value] = m.Groups["guid"].Value;
      }
    }
    return resultado;
  }
}
