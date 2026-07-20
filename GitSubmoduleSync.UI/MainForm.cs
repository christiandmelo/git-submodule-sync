using System.Diagnostics;
using System.Reflection;
using GitSubmoduleSync.Models;
using GitSubmoduleSync.Services;

namespace GitSubmoduleSync.UI;

public partial class MainForm : Form
{
  private readonly ToolLocatorService _toolLocator = new();
  private readonly GitService _gitService = new();
  private readonly NuGetRestoreService _restoreService = new();
  private readonly DependencyGraphService _graphService = new();
  private readonly BuildOrchestrator _buildOrchestrator = new();
  private readonly DevEnvLauncherService _devEnv = new();
  private readonly BuildAllSlnService _buildAllSln = new();

  private ProfilesConfig _config = new();
  private SyncProfile? _perfil;
  private CancellationTokenSource? _cts;
  private Grafo? _ultimoGrafo;
  private Dictionary<string, int> _ondaPorAssembly = new();
  private Dictionary<string, StatusProjeto> _statusPorAssembly = new();
  private Dictionary<string, TreeNode> _nodePorAssembly = new();
  private readonly List<LogEvent> _logCompleto = new();
  private readonly List<ErroCompilacao> _errosAtuais = new();
  private readonly Dictionary<string, ListViewGroup> _gruposErroPorProjeto = new();
  private Stopwatch _cronometro = new();
  private ProgressoBuild? _ultimoProgresso;
  private bool _emExecucao;

  public MainForm()
  {
    InitializeComponent();
    Icon = IconeApp.Obter();

    lvErros.ListViewItemSorter = new ErroPorOndaEProjetoComparer();

    btnConfiguracoes.Click += (_, _) => AbrirConfiguracoes();
    cboPerfil.SelectedIndexChanged += (_, _) => AoTrocarPerfil();
    btnExecutarTudo.Click += async (_, _) => await ExecutarPipelineAsync(sincronizar: true, compilar: true);
    btnSoSincronizar.Click += async (_, _) => await ExecutarPipelineAsync(sincronizar: true, compilar: false);
    btnSoCompilar.Click += async (_, _) => await ExecutarPipelineAsync(sincronizar: false, compilar: true);
    btnCancelar.Click += (_, _) => _cts?.Cancel();
    btnCopiarResumo.Click += (_, _) => { if (txtResumo.Text.Length > 0) Clipboard.SetText(txtResumo.Text); };
    lvErros.DoubleClick += (_, _) => AoDarDuploCliqueEmErro();
    tvOrdem.AfterSelect += (_, e) => AoSelecionarNoDaArvore(e.Node);
    menuRegenerarBuildAllSln.Click += (_, _) => RegenerarBuildAllSln();
    timerProgresso.Tick += (_, _) => AtualizarLabelProgresso();

    Shown += (_, _) => CarregarConfiguracaoInicial();
  }

  private void CarregarConfiguracaoInicial()
  {
    _config = ProfilesConfig.Carregar();
    if (_config.Perfis.Count == 0)
    {
      AbrirConfiguracoes();
      return;
    }
    CarregarPerfisNoCombo();
  }

  private void CarregarPerfisNoCombo()
  {
    cboPerfil.Items.Clear();
    foreach (var p in _config.Perfis) cboPerfil.Items.Add(p.Nome);
    if (cboPerfil.Items.Count == 0) return;

    var indiceAtivo = _config.Perfis.FindIndex(p => p.Nome == _config.PerfilAtivo);
    cboPerfil.SelectedIndex = indiceAtivo >= 0 ? indiceAtivo : 0;
  }

  private void AoTrocarPerfil()
  {
    if (cboPerfil.SelectedItem is not string nome) return;
    _config.PerfilAtivo = nome;
    _config.Salvar();
    AtualizarTelaParaPerfilAtual();
  }

  private void AtualizarTelaParaPerfilAtual()
  {
    _perfil = _config.ObterAtivo();
    _ultimoGrafo = null;

    if (_perfil is null)
    {
      HabilitarAcoes(false);
      return;
    }

    lblPasta.Text = $"Pasta: {_perfil.PastaRaiz}";
    var overrides = _perfil.Projetos.Count(p => p.Branch is not null);
    lblBranchBase.Text = overrides == 0
      ? $"Branch base: {_perfil.BranchBase}"
      : $"Branch base: {_perfil.BranchBase} ({overrides} projeto(s) com branch específica)";
    lblBinCustom.Text = "Bin\\Custom: (calculado na primeira execução)";
    chkIncremental.Checked = _perfil.BuildIncremental;
    chkIgnorarTestes.Checked = _perfil.IgnorarProjetosDeTeste;

    var pre = _toolLocator.Verificar(_perfil);
    if (!pre.Ok)
    {
      HabilitarAcoes(false);
      foreach (var msg in ToolLocatorService.ObterMensagensFalha(pre, _perfil.PastaRaiz))
      {
        AoReceberLog(new LogEvent(NivelLog.Erro, msg));
      }
      return;
    }

    HabilitarAcoes(true);
  }

  private void AbrirConfiguracoes()
  {
    using var form = new ConfigForm(_config);
    form.ShowDialog(this);
    _config = ProfilesConfig.Carregar();
    CarregarPerfisNoCombo();
    AtualizarTelaParaPerfilAtual();
  }

  private void HabilitarAcoes(bool habilitado)
  {
    btnExecutarTudo.Enabled = habilitado && !_emExecucao;
    btnSoSincronizar.Enabled = habilitado && !_emExecucao;
    btnSoCompilar.Enabled = habilitado && !_emExecucao;
  }

  // ==================== Execução ====================

  private async Task ExecutarPipelineAsync(bool sincronizar, bool compilar)
  {
    if (_perfil is null || _emExecucao) return;

    var progressoLog = new Progress<LogEvent>(AoReceberLog);
    var progressoBuild = new Progress<ProgressoBuild>(AoReceberProgresso);

    _emExecucao = true;
    HabilitarAcoes(false);
    btnCancelar.Enabled = true;
    LimparParaNovaExecucao();

    _cts = new CancellationTokenSource();
    var ct = _cts.Token;
    _cronometro = Stopwatch.StartNew();
    timerProgresso.Start();

    var etapas = new List<ResultadoEtapa>();
    ResultadoExecucao? resultadoBuild = null;

    try
    {
      GarantirGitignoreComGssCache(_perfil.PastaRaiz);
      AoReceberLog(new LogEvent(NivelLog.Info, $"Iniciando execução — perfil '{_perfil.Nome}'."));

      if (sincronizar)
      {
        if (_perfil.AtualizarRepositorioPai)
        {
          var etapaPai = await _gitService.AtualizarPaiAsync(_perfil.PastaRaiz, progressoLog, ct);
          etapas.Add(etapaPai);
        }

        var swGit = Stopwatch.StartNew();
        var resultadosGit = await _gitService.SincronizarAsync(_perfil, progressoLog, ct);
        swGit.Stop();
        var semErro = resultadosGit.Count(r => r.Status == StatusSubmodulo.Erro) == 0;
        etapas.Add(new ResultadoEtapa($"Sincronização ({resultadosGit.Count} submódulos)", semErro, swGit.Elapsed));
      }

      if (compilar)
      {
        var msbuild = _toolLocator.LocalizarMsBuild(_perfil.CaminhoMsBuild);
        if (msbuild is null)
        {
          AoReceberLog(new LogEvent(NivelLog.Erro, "MSBuild não encontrado — abortando a compilação."));
        }
        else
        {
          var restore = await _restoreService.RestaurarAsync(_perfil, msbuild, progressoLog, ct);
          etapas.Add(new ResultadoEtapa($"Restore NuGet ({restore.Total} solutions)", restore.Sucesso == restore.Total, restore.Duracao));

          var swGrafo = Stopwatch.StartNew();
          var grafo = _graphService.Montar(_perfil, progressoLog);
          swGrafo.Stop();
          _ultimoGrafo = grafo;
          _ondaPorAssembly = ConstruirMapaOnda(grafo);
          _statusPorAssembly = grafo.Nos.ToDictionary(n => n.AssemblyName, _ => StatusProjeto.Pendente);
          lblBinCustom.Text = $"Bin\\Custom: {grafo.BinCustomResolvido}";
          AtualizarAbaOrdem(grafo);
          etapas.Add(new ResultadoEtapa($"Grafo ({grafo.Nos.Count} projetos, {grafo.Ondas.Count} ondas)", grafo.Ciclos.Count == 0, swGrafo.Elapsed));

          foreach (var aviso in grafo.Avisos) AoReceberLog(new LogEvent(NivelLog.Aviso, aviso));

          var ausentes = _restoreService.VerificarExternas(grafo);
          foreach (var a in ausentes)
          {
            AoReceberLog(new LogEvent(NivelLog.Aviso, $"referência não encontrada: {Path.GetFileName(a.CaminhoAbsoluto)}"));
          }

          resultadoBuild = await _buildOrchestrator.ExecutarAsync(
            grafo, _perfil, msbuild, ignorarIncremental: !chkIncremental.Checked,
            progressoLog, progressoBuild, ct);
          etapas.AddRange(resultadoBuild.Etapas);
        }
      }

      AoReceberLog(new LogEvent(NivelLog.Sucesso, "Execução concluída."));
    }
    catch (OperationCanceledException)
    {
      AoReceberLog(new LogEvent(NivelLog.Aviso, "Execução cancelada pelo usuário."));
    }
    catch (Exception ex)
    {
      AoReceberLog(new LogEvent(NivelLog.Erro, $"erro inesperado: {ex.Message}"));
    }
    finally
    {
      _cronometro.Stop();
      timerProgresso.Stop();
      AtualizarResumo(etapas, resultadoBuild);
      GravarLogEmArquivo();

      _emExecucao = false;
      btnCancelar.Enabled = false;
      HabilitarAcoes(true);
      _cts?.Dispose();
      _cts = null;
    }
  }

  private void LimparParaNovaExecucao()
  {
    rtbLog.Clear();
    lvErros.Items.Clear();
    lvErros.Groups.Clear();
    _gruposErroPorProjeto.Clear();
    _errosAtuais.Clear();
    tabErros.Text = "Erros";
    tvOrdem.Nodes.Clear();
    _nodePorAssembly.Clear();
    lblDependencias.Text = "";
    txtResumo.Clear();
    _logCompleto.Clear();
    progressBar.Value = 0;
    _ultimoProgresso = null;
    lblProgresso.Text = "Executando…";
  }

  // ==================== Log ====================

  private void AoReceberLog(LogEvent evt)
  {
    _logCompleto.Add(evt);

    var prefixo = evt.Projeto ?? evt.Submodulo;
    var linha = prefixo is not null ? $"[{prefixo}] {evt.Mensagem}" : evt.Mensagem;

    rtbLog.SelectionStart = rtbLog.TextLength;
    rtbLog.SelectionLength = 0;
    rtbLog.SelectionColor = CorDoNivel(evt.Nivel);
    rtbLog.AppendText(linha + Environment.NewLine);
    rtbLog.ScrollToCaret();

    if (evt.Projeto is not null)
    {
      AtualizarStatusEArvore(evt);
    }

    if (evt.Nivel == NivelLog.Erro && evt.Projeto is not null)
    {
      var (arquivo, numLinha) = DevEnvLauncherService.ExtrairLocalizacao(evt.Mensagem);
      if (arquivo is not null)
      {
        var onda = _ondaPorAssembly.GetValueOrDefault(evt.Projeto, 0);
        var erro = new ErroCompilacao(evt.Projeto, onda, arquivo, numLinha, evt.Mensagem);
        _errosAtuais.Add(erro);
        AdicionarNaAbaErros(erro);
      }
    }
  }

  private static Color CorDoNivel(NivelLog nivel) => nivel switch
  {
    NivelLog.Detalhe => Color.Gray,
    NivelLog.Info => Color.Gainsboro,
    NivelLog.Sucesso => Color.LimeGreen,
    NivelLog.Aviso => Color.Gold,
    NivelLog.Erro => Color.Tomato,
    _ => Color.Gainsboro,
  };

  private void AdicionarNaAbaErros(ErroCompilacao erro)
  {
    if (!_gruposErroPorProjeto.TryGetValue(erro.Assembly, out var grupo))
    {
      grupo = new ListViewGroup(erro.Assembly);
      _gruposErroPorProjeto[erro.Assembly] = grupo;
      lvErros.Groups.Add(grupo);
    }

    var item = new ListViewItem(erro.Onda.ToString()) { Group = grupo, Tag = erro };
    item.SubItems.Add(erro.Linha?.ToString() ?? "");
    item.SubItems.Add(erro.Arquivo ?? "");
    item.SubItems.Add(erro.LinhaCompleta);
    lvErros.Items.Add(item);
    lvErros.Sort(); // onda mais baixa e' a causa; mostrar antes das consequencias das ondas seguintes

    tabErros.Text = $"Erros ({_errosAtuais.Count})";
  }

  private sealed class ErroPorOndaEProjetoComparer : IComparer<ListViewItem>, System.Collections.IComparer
  {
    public int Compare(ListViewItem? x, ListViewItem? y)
    {
      if (x is null || y is null) return 0;
      var ondaX = int.TryParse(x.SubItems[0].Text, out var ox) ? ox : int.MaxValue;
      var ondaY = int.TryParse(y.SubItems[0].Text, out var oy) ? oy : int.MaxValue;
      if (ondaX != ondaY) return ondaX.CompareTo(ondaY);
      return string.Compare(x.Group?.Header, y.Group?.Header, StringComparison.OrdinalIgnoreCase);
    }

    public int Compare(object? x, object? y) => Compare(x as ListViewItem, y as ListViewItem);
  }

  private void AoDarDuploCliqueEmErro()
  {
    if (lvErros.SelectedItems.Count == 0) return;
    if (lvErros.SelectedItems[0].Tag is not ErroCompilacao erro || erro.Arquivo is null) return;
    _devEnv.AbrirArquivoNaLinha(erro.Arquivo, erro.Linha ?? 1);
  }

  // ==================== Progresso ====================

  private void AoReceberProgresso(ProgressoBuild p)
  {
    _ultimoProgresso = p;
    progressBar.Maximum = Math.Max(p.Total, 1);
    progressBar.Value = Math.Min(p.Concluidos, p.Total);
    AtualizarLabelProgresso();
  }

  private void AtualizarLabelProgresso()
  {
    if (_ultimoProgresso is { } p)
    {
      lblProgresso.Text = $"Onda {p.OndaAtual} de {p.TotalOndas}  ·  {p.Concluidos}/{p.Total} projetos  ·  {_cronometro.Elapsed:mm\\:ss} decorrido";
    }
    else if (_emExecucao)
    {
      lblProgresso.Text = $"Executando…  ·  {_cronometro.Elapsed:mm\\:ss} decorrido";
    }
  }

  // ==================== Aba Ordem de build ====================

  private static Dictionary<string, int> ConstruirMapaOnda(Grafo grafo)
  {
    var mapa = new Dictionary<string, int>(StringComparer.Ordinal);
    for (var i = 0; i < grafo.Ondas.Count; i++)
    {
      foreach (var no in grafo.Ondas[i]) mapa[no.AssemblyName] = i + 1;
    }
    return mapa;
  }

  private void AtualizarAbaOrdem(Grafo grafo)
  {
    tvOrdem.Nodes.Clear();
    _nodePorAssembly.Clear();
    tvOrdem.BeginUpdate();
    for (var i = 0; i < grafo.Ondas.Count; i++)
    {
      var onda = grafo.Ondas[i];
      var noOnda = new TreeNode($"Onda {i + 1} ({onda.Count})");
      foreach (var projeto in onda)
      {
        var noProjeto = new TreeNode(projeto.AssemblyName) { Tag = projeto };
        _nodePorAssembly[projeto.AssemblyName] = noProjeto;
        noOnda.Nodes.Add(noProjeto);
      }
      tvOrdem.Nodes.Add(noOnda);
    }
    tvOrdem.EndUpdate();
  }

  private void AtualizarStatusEArvore(LogEvent evt)
  {
    if (evt.Projeto is null) return;

    StatusProjeto? novoStatus = evt.Nivel switch
    {
      NivelLog.Sucesso when evt.Mensagem.Contains("compilado em") => StatusProjeto.Compilado,
      NivelLog.Erro when evt.Mensagem.Contains("falhou") => StatusProjeto.Falhou,
      NivelLog.Aviso when evt.Mensagem.Contains("bloqueado por dependência") => StatusProjeto.BloqueadoPorDependencia,
      NivelLog.Detalhe when evt.Mensagem.Contains("pulado") => StatusProjeto.PuladoSemAlteracao,
      _ => null,
    };
    if (novoStatus is null) return;

    _statusPorAssembly[evt.Projeto] = novoStatus.Value;
    if (!_nodePorAssembly.TryGetValue(evt.Projeto, out var node)) return;

    var (sufixo, cor) = novoStatus.Value switch
    {
      StatusProjeto.Compilado => (" [OK]", Color.LimeGreen),
      StatusProjeto.Falhou => (" [FALHOU]", Color.Tomato),
      StatusProjeto.BloqueadoPorDependencia => (" [BLOQUEADO]", Color.Gold),
      StatusProjeto.PuladoSemAlteracao => (" [PULADO]", Color.Gray),
      _ => ("", tvOrdem.ForeColor),
    };
    node.Text = evt.Projeto + sufixo;
    node.ForeColor = cor;
  }

  private void AoSelecionarNoDaArvore(TreeNode? node)
  {
    if (node?.Tag is not ProjectNode projeto || _ultimoGrafo is null)
    {
      lblDependencias.Text = "";
      return;
    }
    var deps = _ultimoGrafo.Arestas.TryGetValue(projeto.AssemblyName, out var d) ? d : Array.Empty<string>();
    lblDependencias.Text = deps.Count == 0
      ? $"{projeto.AssemblyName}: sem dependências diretas."
      : $"{projeto.AssemblyName} depende de: {string.Join(", ", deps)}";
  }

  // ==================== Resumo ====================

  private void AtualizarResumo(List<ResultadoEtapa> etapas, ResultadoExecucao? resultadoBuild)
  {
    var sb = new System.Text.StringBuilder();
    sb.AppendLine(new string('=', 68));
    sb.AppendLine($"   RESUMO DA EXECUÇÃO — perfil {_perfil?.Nome}");
    sb.AppendLine(new string('=', 68));
    foreach (var etapa in etapas)
    {
      var status = etapa.Sucesso ? "" : "  [FALHOU]";
      sb.AppendLine($"   {etapa.Nome,-40} {etapa.Duracao:mm\\:ss}{status}");
    }
    sb.AppendLine(new string('-', 68));
    if (resultadoBuild is not null)
    {
      sb.AppendLine($"   Compilados ........ {resultadoBuild.Compilados,-10} Pulados ....... {resultadoBuild.Pulados}");
      sb.AppendLine($"   Falharam .......... {resultadoBuild.Falharam,-10} Bloqueados .... {resultadoBuild.Bloqueados}");
      sb.AppendLine(new string('-', 68));
    }
    sb.AppendLine($"   TEMPO TOTAL ....................... {_cronometro.Elapsed:mm\\:ss}");
    sb.AppendLine(new string('=', 68));
    txtResumo.Text = sb.ToString();
  }

  // ==================== Log em arquivo ====================

  private void GravarLogEmArquivo()
  {
    if (_perfil is null) return;
    try
    {
      var pastaLogs = Path.Combine(_perfil.PastaRaiz, ".gss-cache", "logs");
      Directory.CreateDirectory(pastaLogs);

      var versao = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?";
      var caminho = Path.Combine(pastaLogs, $"{DateTime.Now:yyyyMMdd-HHmmss}.log");

      var sb = new System.Text.StringBuilder();
      sb.AppendLine($"Perfil: {_perfil.Nome}");
      sb.AppendLine($"Pasta raiz: {_perfil.PastaRaiz}");
      sb.AppendLine($"Bin\\Custom resolvido: {_ultimoGrafo?.BinCustomResolvido ?? "(não calculado)"}");
      sb.AppendLine($"Branch base: {_perfil.BranchBase}");
      sb.AppendLine($"Versão da ferramenta: {versao}");
      sb.AppendLine(new string('-', 60));
      foreach (var evt in _logCompleto)
      {
        var prefixo = evt.Projeto ?? evt.Submodulo;
        sb.AppendLine(prefixo is not null ? $"[{evt.Nivel}] [{prefixo}] {evt.Mensagem}" : $"[{evt.Nivel}] {evt.Mensagem}");
      }

      File.WriteAllText(caminho, sb.ToString());

      var arquivos = Directory.GetFiles(pastaLogs, "*.log").OrderByDescending(f => f).ToList();
      foreach (var antigo in arquivos.Skip(20)) File.Delete(antigo);
    }
    catch (IOException) { }
    catch (UnauthorizedAccessException) { }
  }

  private static void GarantirGitignoreComGssCache(string pastaRaiz)
  {
    try
    {
      var caminho = Path.Combine(pastaRaiz, ".gitignore");
      var linhas = File.Exists(caminho) ? File.ReadAllLines(caminho).ToList() : new List<string>();
      if (linhas.Any(l => l.Trim() is ".gss-cache/" or ".gss-cache")) return;
      linhas.Add(".gss-cache/");
      File.WriteAllLines(caminho, linhas);
    }
    catch (IOException) { }
    catch (UnauthorizedAccessException) { }
  }

  // ==================== BuildAll.sln ====================

  private void RegenerarBuildAllSln()
  {
    if (_perfil is null) return;
    if (_ultimoGrafo is null)
    {
      MessageBox.Show(this, "Execute o build ao menos uma vez antes de gerar o BuildAll.sln.", "GitSubmoduleSync",
        MessageBoxButtons.OK, MessageBoxIcon.Information);
      return;
    }

    try
    {
      _buildAllSln.Regenerar(_ultimoGrafo, _perfil.PastaRaiz);
      MessageBox.Show(this, "BuildAll.sln regenerado com sucesso.", "GitSubmoduleSync",
        MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      MessageBox.Show(this, $"Falha ao gerar o BuildAll.sln: {ex.Message}", "GitSubmoduleSync",
        MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
  }
}
