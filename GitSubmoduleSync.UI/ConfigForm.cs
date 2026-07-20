using System.Text.Json;
using GitSubmoduleSync.Models;
using GitSubmoduleSync.Services;

namespace GitSubmoduleSync.UI;

public partial class ConfigForm : Form
{
  private const int ColSubmodulo = 0;
  private const int ColBranch = 1;
  private const int ColOrigem = 2;

  private readonly ProfilesConfig _configOriginal;
  private readonly GitService _gitService = new();

  private List<SyncProfile> _perfis;
  private SyncProfile? _perfilAtual;
  private bool _carregandoCampos;

  /// <summary>Somente para o WinForms Designer — exige um construtor sem parâmetros para instanciar em tempo de design.</summary>
  public ConfigForm() : this(new ProfilesConfig())
  {
  }

  public ConfigForm(ProfilesConfig config)
  {
    InitializeComponent();
    Icon = IconeApp.Obter();
    _configOriginal = config;

    var json = JsonSerializer.Serialize(config.Perfis);
    _perfis = JsonSerializer.Deserialize<List<SyncProfile>>(json) ?? new List<SyncProfile>();

    ConfigurarGrid();

    cboPerfil.SelectedIndexChanged += (_, _) => AoTrocarSelecaoDoCombo();
    btnNovo.Click += (_, _) => Novo();
    btnDuplicar.Click += (_, _) => Duplicar();
    btnExcluir.Click += (_, _) => Excluir();
    btnProcurar.Click += (_, _) => Procurar();
    txtPastaRaiz.Leave += (_, _) => ValidarPastaRaiz();
    btnLerProjetos.Click += async (_, _) => await LerProjetosAsync();
    btnRestaurarTodos.Click += (_, _) => RestaurarTodosParaABase();
    btnSalvar.Click += (_, _) => Salvar();

    RecarregarCombo(_configOriginal.PerfilAtivo);
  }

  // ==================== Perfis ====================

  private void RecarregarCombo(string? selecionar)
  {
    cboPerfil.Items.Clear();
    foreach (var p in _perfis) cboPerfil.Items.Add(p.Nome);

    if (cboPerfil.Items.Count == 0)
    {
      _perfilAtual = null;
      LimparCampos();
      return;
    }

    var indice = selecionar is not null ? _perfis.FindIndex(p => p.Nome == selecionar) : -1;
    cboPerfil.SelectedIndex = indice >= 0 ? indice : 0;
  }

  private void AoTrocarSelecaoDoCombo()
  {
    if (cboPerfil.SelectedIndex < 0) return;
    _perfilAtual = _perfis[cboPerfil.SelectedIndex];
    CarregarCamposDoPerfil();
  }

  private void Novo()
  {
    using var prompt = new PromptForm("Novo perfil", "Nome do perfil:");
    if (prompt.ShowDialog(this) != DialogResult.OK || prompt.Valor.Length == 0) return;
    if (_perfis.Any(p => string.Equals(p.Nome, prompt.Valor, StringComparison.OrdinalIgnoreCase)))
    {
      MessageBox.Show(this, "Já existe um perfil com esse nome.", "GitSubmoduleSync", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      return;
    }

    var novo = new SyncProfile { Nome = prompt.Valor };
    _perfis.Add(novo);
    RecarregarCombo(novo.Nome);
  }

  private void Duplicar()
  {
    if (_perfilAtual is null) return;
    CommitCamposParaPerfilAtual();

    using var prompt = new PromptForm("Duplicar perfil", "Nome do novo perfil:", $"{_perfilAtual.Nome} - cópia");
    if (prompt.ShowDialog(this) != DialogResult.OK || prompt.Valor.Length == 0) return;
    if (_perfis.Any(p => string.Equals(p.Nome, prompt.Valor, StringComparison.OrdinalIgnoreCase)))
    {
      MessageBox.Show(this, "Já existe um perfil com esse nome.", "GitSubmoduleSync", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      return;
    }

    var json = JsonSerializer.Serialize(_perfilAtual);
    var copia = JsonSerializer.Deserialize<SyncProfile>(json)!;
    copia.Nome = prompt.Valor;
    _perfis.Add(copia);
    RecarregarCombo(copia.Nome);
  }

  private void Excluir()
  {
    if (_perfilAtual is null) return;
    var resp = MessageBox.Show(this, $"Excluir o perfil '{_perfilAtual.Nome}'?", "GitSubmoduleSync",
      MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
    if (resp != DialogResult.Yes) return;

    _perfis.Remove(_perfilAtual);
    _perfilAtual = null;
    RecarregarCombo(null);
  }

  // ==================== Campos ====================

  private void CarregarCamposDoPerfil()
  {
    if (_perfilAtual is null) return;
    _carregandoCampos = true;

    txtNome.Text = _perfilAtual.Nome;
    txtPastaRaiz.Text = _perfilAtual.PastaRaiz;
    txtBranchBase.Text = _perfilAtual.BranchBase;
    chkAtualizarPai.Checked = _perfilAtual.AtualizarRepositorioPai;
    chkIgnorarTestes.Checked = _perfilAtual.IgnorarProjetosDeTeste;
    chkBuildIncremental.Checked = _perfilAtual.BuildIncremental;
    numParalelismoGit.Value = Math.Clamp(_perfilAtual.GrauParalelismoGit <= 0 ? 4 : _perfilAtual.GrauParalelismoGit, numParalelismoGit.Minimum, numParalelismoGit.Maximum);
    numParalelismoBuild.Value = Math.Clamp(_perfilAtual.GrauParalelismoBuild, numParalelismoBuild.Minimum, numParalelismoBuild.Maximum);
    lblAvisoPasta.Text = "";

    PopularGrid();
    _carregandoCampos = false;
  }

  private void CommitCamposParaPerfilAtual()
  {
    if (_perfilAtual is null || _carregandoCampos) return;
    _perfilAtual.Nome = txtNome.Text.Trim();
    _perfilAtual.PastaRaiz = txtPastaRaiz.Text.Trim();
    _perfilAtual.BranchBase = txtBranchBase.Text.Trim();
    _perfilAtual.AtualizarRepositorioPai = chkAtualizarPai.Checked;
    _perfilAtual.IgnorarProjetosDeTeste = chkIgnorarTestes.Checked;
    _perfilAtual.BuildIncremental = chkBuildIncremental.Checked;
    _perfilAtual.GrauParalelismoGit = (int)numParalelismoGit.Value;
    _perfilAtual.GrauParalelismoBuild = (int)numParalelismoBuild.Value;
  }

  private void LimparCampos()
  {
    _carregandoCampos = true;
    txtNome.Text = "";
    txtPastaRaiz.Text = "";
    txtBranchBase.Text = "";
    lblAvisoPasta.Text = "";
    grid.Rows.Clear();
    _carregandoCampos = false;
  }

  private void Procurar()
  {
    using var dlg = new FolderBrowserDialog { Description = "Selecione a pasta raiz do cliente" };
    if (!string.IsNullOrWhiteSpace(txtPastaRaiz.Text) && Directory.Exists(txtPastaRaiz.Text))
    {
      dlg.SelectedPath = txtPastaRaiz.Text;
    }
    if (dlg.ShowDialog(this) == DialogResult.OK)
    {
      txtPastaRaiz.Text = dlg.SelectedPath;
      ValidarPastaRaiz();
    }
  }

  private void ValidarPastaRaiz()
  {
    var pasta = txtPastaRaiz.Text.Trim();
    if (pasta.Length == 0) { lblAvisoPasta.Text = ""; return; }

    if (!Directory.Exists(pasta))
    {
      lblAvisoPasta.Text = "Aviso: a pasta não existe.";
    }
    else if (!File.Exists(Path.Combine(pasta, ".gitmodules")))
    {
      lblAvisoPasta.Text = "Aviso: a pasta não contém .gitmodules (pode ser um perfil configurado antes do clone).";
    }
    else
    {
      lblAvisoPasta.Text = "";
    }
  }

  // ==================== Grid de projetos ====================

  private void ConfigurarGrid()
  {
    grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSubmodulo", HeaderText = "Projeto", ReadOnly = true });
    grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBranch", HeaderText = "Branch" });
    grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrigem", HeaderText = "Origem", ReadOnly = true, FillWeight = 60 });

    grid.CellEndEdit += Grid_CellEndEdit;
  }

  private void PopularGrid()
  {
    grid.Rows.Clear();
    if (_perfilAtual is null) return;

    foreach (var projeto in _perfilAtual.Projetos)
    {
      AdicionarLinhaNaGrid(projeto.Submodulo, projeto.Branch);
    }
  }

  private void AdicionarLinhaNaGrid(string submodulo, string? branchOverride)
  {
    var idx = grid.Rows.Add();
    var linha = grid.Rows[idx];
    linha.Cells[ColSubmodulo].Value = submodulo;
    AtualizarLinhaDeBranch(linha, branchOverride);
  }

  private void AtualizarLinhaDeBranch(DataGridViewRow linha, string? branchOverride)
  {
    var branchBase = _perfilAtual?.BranchBase ?? "";
    if (branchOverride is null)
    {
      linha.Cells[ColBranch].Value = branchBase;
      linha.Cells[ColOrigem].Value = "(base)";
      linha.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Regular);
      linha.Cells[ColOrigem].Style.ForeColor = Color.Gray;
    }
    else
    {
      linha.Cells[ColBranch].Value = branchOverride;
      linha.Cells[ColOrigem].Value = "(customizado)";
      linha.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
      linha.Cells[ColOrigem].Style.ForeColor = grid.ForeColor;
    }
  }

  // Branch é sempre um campo de texto livre (branch ainda não publicada é um caso válido).

  private void Grid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
  {
    if (e.ColumnIndex != ColBranch || _perfilAtual is null) return;

    var linha = grid.Rows[e.RowIndex];
    var submodulo = (string)linha.Cells[ColSubmodulo].Value!;
    var texto = (linha.Cells[ColBranch].Value as string ?? "").Trim();

    var config = _perfilAtual.Projetos.FirstOrDefault(p => p.Submodulo == submodulo);
    if (config is null)
    {
      config = new ProjetoConfig { Submodulo = submodulo };
      _perfilAtual.Projetos.Add(config);
    }

    // Digitar de volta o nome exato da branch base equivale a "restaurar para a base":
    // volta a herdar (Branch = null) em vez de fixar um valor igual ao da base.
    config.Branch = (texto.Length == 0 || string.Equals(texto, _perfilAtual.BranchBase, StringComparison.Ordinal))
      ? null
      : texto;

    AtualizarLinhaDeBranch(linha, config.Branch);
  }

  private async Task LerProjetosAsync()
  {
    if (_perfilAtual is null) return;
    CommitCamposParaPerfilAtual();

    var pastaRaiz = _perfilAtual.PastaRaiz;
    if (!Directory.Exists(pastaRaiz) || !File.Exists(Path.Combine(pastaRaiz, ".gitmodules")))
    {
      MessageBox.Show(this, "Pasta raiz inválida ou sem .gitmodules — não é possível ler os projetos.",
        "GitSubmoduleSync", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      return;
    }

    var textoOriginal = btnLerProjetos.Text;
    IReadOnlyList<SubmoduloInfo> descobertos;
    try
    {
      btnLerProjetos.Enabled = false;
      btnLerProjetos.Text = "Lendo…";
      Cursor.Current = Cursors.WaitCursor;
      descobertos = await _gitService.DescobrirAsync(pastaRaiz, CancellationToken.None);
    }
    finally
    {
      btnLerProjetos.Enabled = true;
      btnLerProjetos.Text = textoOriginal;
      Cursor.Current = Cursors.Default;
    }

    var nomesAtuais = descobertos.Select(d => d.Nome).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var removidosComOverride = _perfilAtual.Projetos
      .Where(p => p.Branch is not null && !nomesAtuais.Contains(p.Submodulo))
      .ToList();

    if (removidosComOverride.Count > 0)
    {
      var lista = string.Join("\n", removidosComOverride.Select(p => $"  {p.Submodulo}  ({p.Branch})"));
      var resp = MessageBox.Show(this,
        $"Os projetos abaixo têm uma branch específica configurada, mas não existem mais em .gitmodules:\n\n{lista}\n\nDescartar essas configurações?",
        "Ler projetos", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
      if (resp != DialogResult.Yes) return;
    }

    var novaLista = new List<ProjetoConfig>();
    foreach (var d in descobertos)
    {
      var existente = _perfilAtual.Projetos.FirstOrDefault(p => string.Equals(p.Submodulo, d.Nome, StringComparison.OrdinalIgnoreCase));
      novaLista.Add(new ProjetoConfig { Submodulo = d.Nome, Branch = existente?.Branch });
    }
    _perfilAtual.Projetos = novaLista;
    PopularGrid();
  }

  private void RestaurarTodosParaABase()
  {
    if (_perfilAtual is null) return;
    foreach (var p in _perfilAtual.Projetos) p.Branch = null;
    PopularGrid();
  }

  // ==================== Salvar / Cancelar ====================

  private void Salvar()
  {
    CommitCamposParaPerfilAtual();

    if (_perfilAtual is not null)
    {
      if (string.IsNullOrWhiteSpace(_perfilAtual.Nome))
      {
        MessageBox.Show(this, "O nome do perfil é obrigatório.", "GitSubmoduleSync", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }
      if (_perfis.Count(p => string.Equals(p.Nome, _perfilAtual.Nome, StringComparison.OrdinalIgnoreCase)) > 1)
      {
        MessageBox.Show(this, "Já existe outro perfil com esse nome.", "GitSubmoduleSync", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }
      if (string.IsNullOrWhiteSpace(_perfilAtual.BranchBase) || _perfilAtual.BranchBase.Contains(' '))
      {
        MessageBox.Show(this, "A branch base é obrigatória e não pode conter espaços.", "GitSubmoduleSync", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }
    }

    _configOriginal.Perfis = _perfis;
    _configOriginal.PerfilAtivo = _perfis.Any(p => p.Nome == _configOriginal.PerfilAtivo)
      ? _configOriginal.PerfilAtivo
      : (_perfilAtual?.Nome ?? _perfis.FirstOrDefault()?.Nome ?? "");
    _configOriginal.Salvar();

    DialogResult = DialogResult.OK;
    Close();
  }
}
