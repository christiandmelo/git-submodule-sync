namespace GitSubmoduleSync.UI;

partial class MainForm
{
  private System.ComponentModel.IContainer components = null;

  protected override void Dispose(bool disposing)
  {
    if (disposing && (components != null))
    {
      components.Dispose();
    }
    base.Dispose(disposing);
  }

  #region Windows Form Designer generated code

  private System.Windows.Forms.MenuStrip menuStrip;
  private System.Windows.Forms.ToolStripMenuItem menuFerramentas;
  private System.Windows.Forms.ToolStripMenuItem menuRegenerarBuildAllSln;

  private System.Windows.Forms.Panel painelTopo;
  private System.Windows.Forms.ComboBox cboPerfil;
  private System.Windows.Forms.Button btnConfiguracoes;
  private System.Windows.Forms.Label lblPasta;
  private System.Windows.Forms.Label lblBranchBase;
  private System.Windows.Forms.Label lblBinCustom;
  private System.Windows.Forms.CheckBox chkIncremental;
  private System.Windows.Forms.CheckBox chkIgnorarTestes;

  private System.Windows.Forms.Panel painelBotoes;
  private System.Windows.Forms.Button btnExecutarTudo;
  private System.Windows.Forms.Button btnSoSincronizar;
  private System.Windows.Forms.Button btnSoCompilar;
  private System.Windows.Forms.Button btnCancelar;

  private System.Windows.Forms.Panel painelProgresso;
  private System.Windows.Forms.Label lblProgresso;
  private System.Windows.Forms.ProgressBar progressBar;

  private System.Windows.Forms.TabControl tabControl;
  private System.Windows.Forms.TabPage tabLog;
  private System.Windows.Forms.RichTextBox rtbLog;
  private System.Windows.Forms.TabPage tabErros;
  private System.Windows.Forms.ListView lvErros;
  private System.Windows.Forms.TabPage tabOrdem;
  private System.Windows.Forms.TreeView tvOrdem;
  private System.Windows.Forms.Label lblDependencias;
  private System.Windows.Forms.TabPage tabResumo;
  private System.Windows.Forms.TextBox txtResumo;
  private System.Windows.Forms.Button btnCopiarResumo;

  private System.Windows.Forms.Timer timerProgresso;

  private void InitializeComponent()
  {
    this.components = new System.ComponentModel.Container();

    this.menuStrip = new System.Windows.Forms.MenuStrip();
    this.menuFerramentas = new System.Windows.Forms.ToolStripMenuItem("Ferramentas");
    this.menuRegenerarBuildAllSln = new System.Windows.Forms.ToolStripMenuItem("Regenerar BuildAll.sln");
    this.menuFerramentas.DropDownItems.Add(this.menuRegenerarBuildAllSln);
    this.menuStrip.Items.Add(this.menuFerramentas);

    this.painelTopo = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Top, Height = 130 };
    this.cboPerfil = new System.Windows.Forms.ComboBox { Location = new System.Drawing.Point(12, 10), Width = 220, DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList };
    this.btnConfiguracoes = new System.Windows.Forms.Button { Text = "Configurações…", Location = new System.Drawing.Point(240, 9), Width = 130 };
    this.lblPasta = new System.Windows.Forms.Label { Location = new System.Drawing.Point(12, 40), AutoSize = true, Text = "Pasta: —" };
    this.lblBranchBase = new System.Windows.Forms.Label { Location = new System.Drawing.Point(12, 60), AutoSize = true, Text = "Branch base: —" };
    this.lblBinCustom = new System.Windows.Forms.Label { Location = new System.Drawing.Point(12, 80), AutoSize = true, Text = "Bin\\Custom: —" };
    this.chkIncremental = new System.Windows.Forms.CheckBox { Text = "Incremental", Location = new System.Drawing.Point(12, 103), AutoSize = true, Checked = true };
    this.chkIgnorarTestes = new System.Windows.Forms.CheckBox { Text = "Ignorar testes", Location = new System.Drawing.Point(120, 103), AutoSize = true, Checked = true };
    this.painelTopo.Controls.AddRange(new System.Windows.Forms.Control[] {
      this.cboPerfil, this.btnConfiguracoes, this.lblPasta, this.lblBranchBase, this.lblBinCustom, this.chkIncremental, this.chkIgnorarTestes
    });

    this.painelBotoes = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Top, Height = 40 };
    this.btnExecutarTudo = new System.Windows.Forms.Button { Text = "Executar tudo", Location = new System.Drawing.Point(12, 5), Width = 120 };
    this.btnSoSincronizar = new System.Windows.Forms.Button { Text = "Só sincronizar", Location = new System.Drawing.Point(138, 5), Width = 120 };
    this.btnSoCompilar = new System.Windows.Forms.Button { Text = "Só compilar", Location = new System.Drawing.Point(264, 5), Width = 120 };
    this.btnCancelar = new System.Windows.Forms.Button { Text = "■", Location = new System.Drawing.Point(390, 5), Width = 40, Enabled = false };
    this.painelBotoes.Controls.AddRange(new System.Windows.Forms.Control[] {
      this.btnExecutarTudo, this.btnSoSincronizar, this.btnSoCompilar, this.btnCancelar
    });

    this.painelProgresso = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Top, Height = 40 };
    this.lblProgresso = new System.Windows.Forms.Label { Location = new System.Drawing.Point(12, 4), AutoSize = true, Text = "Pronto." };
    this.progressBar = new System.Windows.Forms.ProgressBar { Location = new System.Drawing.Point(12, 20), Width = 860, Height = 14 };
    this.painelProgresso.Controls.AddRange(new System.Windows.Forms.Control[] { this.lblProgresso, this.progressBar });

    this.rtbLog = new System.Windows.Forms.RichTextBox
    {
      Dock = System.Windows.Forms.DockStyle.Fill, ReadOnly = true, BackColor = System.Drawing.Color.Black,
      ForeColor = System.Drawing.Color.Gainsboro, Font = new System.Drawing.Font("Consolas", 9.5f), BorderStyle = System.Windows.Forms.BorderStyle.None,
    };
    this.tabLog = new System.Windows.Forms.TabPage("Log");
    this.tabLog.Controls.Add(this.rtbLog);

    this.lvErros = new System.Windows.Forms.ListView { Dock = System.Windows.Forms.DockStyle.Fill, View = System.Windows.Forms.View.Details, FullRowSelect = true, GridLines = true };
    this.lvErros.Columns.Add("Onda", 60);
    this.lvErros.Columns.Add("Linha", 60);
    this.lvErros.Columns.Add("Arquivo", 320);
    this.lvErros.Columns.Add("Mensagem", 500);
    this.tabErros = new System.Windows.Forms.TabPage("Erros");
    this.tabErros.Controls.Add(this.lvErros);

    this.tvOrdem = new System.Windows.Forms.TreeView { Dock = System.Windows.Forms.DockStyle.Fill };
    this.lblDependencias = new System.Windows.Forms.Label { Dock = System.Windows.Forms.DockStyle.Bottom, Height = 40, Text = "" };
    this.tabOrdem = new System.Windows.Forms.TabPage("Ordem de build");
    this.tabOrdem.Controls.Add(this.tvOrdem);
    this.tabOrdem.Controls.Add(this.lblDependencias);

    this.txtResumo = new System.Windows.Forms.TextBox
    {
      Dock = System.Windows.Forms.DockStyle.Fill, Multiline = true, ReadOnly = true,
      ScrollBars = System.Windows.Forms.ScrollBars.Vertical, Font = new System.Drawing.Font("Consolas", 9.5f),
    };
    this.btnCopiarResumo = new System.Windows.Forms.Button { Text = "Copiar resumo", Dock = System.Windows.Forms.DockStyle.Bottom, Height = 30 };
    this.tabResumo = new System.Windows.Forms.TabPage("Resumo");
    this.tabResumo.Controls.Add(this.txtResumo);
    this.tabResumo.Controls.Add(this.btnCopiarResumo);

    this.tabControl = new System.Windows.Forms.TabControl { Dock = System.Windows.Forms.DockStyle.Fill };
    this.tabControl.TabPages.AddRange(new System.Windows.Forms.TabPage[] { this.tabLog, this.tabErros, this.tabOrdem, this.tabResumo });

    this.timerProgresso = new System.Windows.Forms.Timer(this.components) { Interval = 1000 };

    this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
    this.ClientSize = new System.Drawing.Size(900, 600);
    this.Text = "GitSubmoduleSync";
    this.Controls.Add(this.tabControl);
    this.Controls.Add(this.painelProgresso);
    this.Controls.Add(this.painelBotoes);
    this.Controls.Add(this.painelTopo);
    this.Controls.Add(this.menuStrip);
    this.MainMenuStrip = this.menuStrip;
    this.MinimumSize = new System.Drawing.Size(700, 450);
  }

  #endregion
}
