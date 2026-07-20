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
  private System.Windows.Forms.Label lblCronometro;

  private System.Windows.Forms.Panel painelProgresso;
  private System.Windows.Forms.Label lblProgressoGit;
  private System.Windows.Forms.ProgressBar progressBarGit;
  private System.Windows.Forms.Label lblProgressoBuild;
  private System.Windows.Forms.ProgressBar progressBarBuild;

  private System.Windows.Forms.TabControl tabControl;
  private System.Windows.Forms.TabPage tabLog;
  private System.Windows.Forms.RichTextBox rtbLog;
  private System.Windows.Forms.TabPage tabErros;
  private System.Windows.Forms.ListView lvErros;
  private System.Windows.Forms.ColumnHeader colOnda;
  private System.Windows.Forms.ColumnHeader colLinha;
  private System.Windows.Forms.ColumnHeader colArquivo;
  private System.Windows.Forms.ColumnHeader colMensagem;
  private System.Windows.Forms.TabPage tabOrdem;
  private System.Windows.Forms.TreeView tvOrdem;
  private System.Windows.Forms.Label lblDependencias;
  private System.Windows.Forms.TabPage tabResumo;
  private System.Windows.Forms.TextBox txtResumo;
  private System.Windows.Forms.Button btnCopiarResumo;

  private System.Windows.Forms.Timer timerProgresso;

  private void InitializeComponent()
  {
    components = new System.ComponentModel.Container();
    menuStrip = new MenuStrip();
    menuFerramentas = new ToolStripMenuItem();
    menuRegenerarBuildAllSln = new ToolStripMenuItem();
    painelTopo = new Panel();
    chkIgnorarTestes = new CheckBox();
    chkIncremental = new CheckBox();
    lblBinCustom = new Label();
    lblBranchBase = new Label();
    lblPasta = new Label();
    btnConfiguracoes = new Button();
    cboPerfil = new ComboBox();
    painelBotoes = new Panel();
    lblCronometro = new Label();
    btnCancelar = new Button();
    btnSoCompilar = new Button();
    btnSoSincronizar = new Button();
    btnExecutarTudo = new Button();
    painelProgresso = new Panel();
    progressBarGit = new ProgressBar();
    lblProgressoGit = new Label();
    progressBarBuild = new ProgressBar();
    lblProgressoBuild = new Label();
    tabControl = new TabControl();
    tabLog = new TabPage();
    rtbLog = new RichTextBox();
    tabErros = new TabPage();
    lvErros = new ListView();
    colOnda = new ColumnHeader();
    colLinha = new ColumnHeader();
    colArquivo = new ColumnHeader();
    colMensagem = new ColumnHeader();
    tabOrdem = new TabPage();
    tvOrdem = new TreeView();
    lblDependencias = new Label();
    tabResumo = new TabPage();
    txtResumo = new TextBox();
    btnCopiarResumo = new Button();
    timerProgresso = new System.Windows.Forms.Timer(components);
    menuStrip.SuspendLayout();
    painelTopo.SuspendLayout();
    painelBotoes.SuspendLayout();
    painelProgresso.SuspendLayout();
    tabControl.SuspendLayout();
    tabLog.SuspendLayout();
    tabErros.SuspendLayout();
    tabOrdem.SuspendLayout();
    tabResumo.SuspendLayout();
    SuspendLayout();
    // 
    // menuStrip
    // 
    menuStrip.ImageScalingSize = new Size(20, 20);
    menuStrip.Items.AddRange(new ToolStripItem[] { menuFerramentas });
    menuStrip.Location = new Point(0, 0);
    menuStrip.Name = "menuStrip";
    menuStrip.Padding = new Padding(7, 3, 0, 3);
    menuStrip.Size = new Size(1437, 30);
    menuStrip.TabIndex = 0;
    // 
    // menuFerramentas
    // 
    menuFerramentas.DropDownItems.AddRange(new ToolStripItem[] { menuRegenerarBuildAllSln });
    menuFerramentas.Name = "menuFerramentas";
    menuFerramentas.Size = new Size(104, 24);
    menuFerramentas.Text = "Ferramentas";
    // 
    // menuRegenerarBuildAllSln
    // 
    menuRegenerarBuildAllSln.Name = "menuRegenerarBuildAllSln";
    menuRegenerarBuildAllSln.Size = new Size(237, 26);
    menuRegenerarBuildAllSln.Text = "Regenerar BuildAll.sln";
    // 
    // painelTopo
    // 
    painelTopo.Controls.Add(chkIgnorarTestes);
    painelTopo.Controls.Add(chkIncremental);
    painelTopo.Controls.Add(lblBinCustom);
    painelTopo.Controls.Add(lblBranchBase);
    painelTopo.Controls.Add(lblPasta);
    painelTopo.Controls.Add(btnConfiguracoes);
    painelTopo.Controls.Add(cboPerfil);
    painelTopo.Dock = DockStyle.Top;
    painelTopo.Location = new Point(0, 30);
    painelTopo.Margin = new Padding(3, 4, 3, 4);
    painelTopo.Name = "painelTopo";
    painelTopo.Size = new Size(1437, 121);
    painelTopo.TabIndex = 1;
    // 
    // chkIgnorarTestes
    // 
    chkIgnorarTestes.AutoSize = true;
    chkIgnorarTestes.Checked = true;
    chkIgnorarTestes.CheckState = CheckState.Checked;
    chkIgnorarTestes.Location = new Point(139, 93);
    chkIgnorarTestes.Margin = new Padding(3, 4, 3, 4);
    chkIgnorarTestes.Name = "chkIgnorarTestes";
    chkIgnorarTestes.Size = new Size(121, 24);
    chkIgnorarTestes.TabIndex = 6;
    chkIgnorarTestes.Text = "Ignorar testes";
    // 
    // chkIncremental
    // 
    chkIncremental.AutoSize = true;
    chkIncremental.Checked = true;
    chkIncremental.CheckState = CheckState.Checked;
    chkIncremental.Location = new Point(16, 93);
    chkIncremental.Margin = new Padding(3, 4, 3, 4);
    chkIncremental.Name = "chkIncremental";
    chkIncremental.Size = new Size(109, 24);
    chkIncremental.TabIndex = 5;
    chkIncremental.Text = "Incremental";
    // 
    // lblBinCustom
    // 
    lblBinCustom.AutoSize = true;
    lblBinCustom.Location = new Point(14, 69);
    lblBinCustom.Name = "lblBinCustom";
    lblBinCustom.Size = new Size(108, 20);
    lblBinCustom.TabIndex = 4;
    lblBinCustom.Text = "Bin\\Custom: —";
    // 
    // lblBranchBase
    // 
    lblBranchBase.AutoSize = true;
    lblBranchBase.Location = new Point(14, 45);
    lblBranchBase.Name = "lblBranchBase";
    lblBranchBase.Size = new Size(111, 20);
    lblBranchBase.TabIndex = 3;
    lblBranchBase.Text = "Branch base: —";
    // 
    // lblPasta
    // 
    lblPasta.AutoSize = true;
    lblPasta.Location = new Point(343, 46);
    lblPasta.Name = "lblPasta";
    lblPasta.Size = new Size(65, 20);
    lblPasta.TabIndex = 2;
    lblPasta.Text = "Pasta: —";
    // 
    // btnConfiguracoes
    // 
    btnConfiguracoes.Location = new Point(343, 13);
    btnConfiguracoes.Margin = new Padding(3, 4, 3, 4);
    btnConfiguracoes.Name = "btnConfiguracoes";
    btnConfiguracoes.Size = new Size(149, 29);
    btnConfiguracoes.TabIndex = 1;
    btnConfiguracoes.Text = "Configurações…";
    btnConfiguracoes.UseVisualStyleBackColor = true;
    // 
    // cboPerfil
    // 
    cboPerfil.DropDownStyle = ComboBoxStyle.DropDownList;
    cboPerfil.Location = new Point(14, 13);
    cboPerfil.Margin = new Padding(3, 4, 3, 4);
    cboPerfil.Name = "cboPerfil";
    cboPerfil.Size = new Size(323, 28);
    cboPerfil.TabIndex = 0;
    // 
    // painelBotoes
    // 
    painelBotoes.Controls.Add(lblCronometro);
    painelBotoes.Controls.Add(btnCancelar);
    painelBotoes.Controls.Add(btnSoCompilar);
    painelBotoes.Controls.Add(btnSoSincronizar);
    painelBotoes.Controls.Add(btnExecutarTudo);
    painelBotoes.Dock = DockStyle.Top;
    painelBotoes.Location = new Point(0, 151);
    painelBotoes.Margin = new Padding(3, 4, 3, 4);
    painelBotoes.Name = "painelBotoes";
    painelBotoes.Size = new Size(1437, 53);
    painelBotoes.TabIndex = 2;
    // 
    // btnCancelar
    // 
    btnCancelar.Enabled = false;
    btnCancelar.Location = new Point(446, 7);
    btnCancelar.Margin = new Padding(3, 4, 3, 4);
    btnCancelar.Name = "btnCancelar";
    btnCancelar.Size = new Size(46, 36);
    btnCancelar.TabIndex = 3;
    btnCancelar.Text = "■";
    btnCancelar.UseVisualStyleBackColor = true;
    //
    // lblCronometro
    //
    lblCronometro.AutoSize = true;
    lblCronometro.Location = new Point(504, 17);
    lblCronometro.Name = "lblCronometro";
    lblCronometro.Size = new Size(56, 20);
    lblCronometro.TabIndex = 4;
    lblCronometro.Text = "";
    //
    // btnSoCompilar
    // 
    btnSoCompilar.Location = new Point(302, 7);
    btnSoCompilar.Margin = new Padding(3, 4, 3, 4);
    btnSoCompilar.Name = "btnSoCompilar";
    btnSoCompilar.Size = new Size(137, 36);
    btnSoCompilar.TabIndex = 2;
    btnSoCompilar.Text = "Só compilar";
    btnSoCompilar.UseVisualStyleBackColor = true;
    // 
    // btnSoSincronizar
    // 
    btnSoSincronizar.Location = new Point(158, 7);
    btnSoSincronizar.Margin = new Padding(3, 4, 3, 4);
    btnSoSincronizar.Name = "btnSoSincronizar";
    btnSoSincronizar.Size = new Size(137, 36);
    btnSoSincronizar.TabIndex = 1;
    btnSoSincronizar.Text = "Só sincronizar";
    btnSoSincronizar.UseVisualStyleBackColor = true;
    // 
    // btnExecutarTudo
    // 
    btnExecutarTudo.Location = new Point(14, 7);
    btnExecutarTudo.Margin = new Padding(3, 4, 3, 4);
    btnExecutarTudo.Name = "btnExecutarTudo";
    btnExecutarTudo.Size = new Size(137, 36);
    btnExecutarTudo.TabIndex = 0;
    btnExecutarTudo.Text = "Executar tudo";
    btnExecutarTudo.UseVisualStyleBackColor = true;
    //
    // painelProgresso
    //
    painelProgresso.Controls.Add(progressBarBuild);
    painelProgresso.Controls.Add(lblProgressoBuild);
    painelProgresso.Controls.Add(progressBarGit);
    painelProgresso.Controls.Add(lblProgressoGit);
    painelProgresso.Dock = DockStyle.Top;
    painelProgresso.Location = new Point(0, 204);
    painelProgresso.Margin = new Padding(3, 4, 3, 4);
    painelProgresso.Name = "painelProgresso";
    painelProgresso.Size = new Size(1437, 97);
    painelProgresso.TabIndex = 3;
    //
    // lblProgressoGit
    //
    lblProgressoGit.AutoSize = true;
    lblProgressoGit.Location = new Point(14, 5);
    lblProgressoGit.Name = "lblProgressoGit";
    lblProgressoGit.Size = new Size(56, 20);
    lblProgressoGit.TabIndex = 0;
    lblProgressoGit.Text = "Git: pronto.";
    //
    // progressBarGit
    //
    progressBarGit.Location = new Point(14, 27);
    progressBarGit.Margin = new Padding(3, 4, 3, 4);
    progressBarGit.Name = "progressBarGit";
    progressBarGit.Size = new Size(1411, 19);
    progressBarGit.TabIndex = 1;
    //
    // lblProgressoBuild
    //
    lblProgressoBuild.AutoSize = true;
    lblProgressoBuild.Location = new Point(14, 51);
    lblProgressoBuild.Name = "lblProgressoBuild";
    lblProgressoBuild.Size = new Size(56, 20);
    lblProgressoBuild.TabIndex = 2;
    lblProgressoBuild.Text = "Build: pronto.";
    //
    // progressBarBuild
    //
    progressBarBuild.Location = new Point(14, 73);
    progressBarBuild.Margin = new Padding(3, 4, 3, 4);
    progressBarBuild.Name = "progressBarBuild";
    progressBarBuild.Size = new Size(1411, 19);
    progressBarBuild.TabIndex = 3;
    // 
    // tabControl
    // 
    tabControl.Controls.Add(tabLog);
    tabControl.Controls.Add(tabErros);
    tabControl.Controls.Add(tabOrdem);
    tabControl.Controls.Add(tabResumo);
    tabControl.Dock = DockStyle.Fill;
    tabControl.Location = new Point(0, 301);
    tabControl.Margin = new Padding(3, 4, 3, 4);
    tabControl.Name = "tabControl";
    tabControl.SelectedIndex = 0;
    tabControl.Size = new Size(1437, 499);
    tabControl.TabIndex = 4;
    // 
    // tabLog
    // 
    tabLog.Controls.Add(rtbLog);
    tabLog.Location = new Point(4, 29);
    tabLog.Margin = new Padding(3, 4, 3, 4);
    tabLog.Name = "tabLog";
    tabLog.Padding = new Padding(3, 4, 3, 4);
    tabLog.Size = new Size(1429, 510);
    tabLog.TabIndex = 0;
    tabLog.Text = "Log";
    tabLog.UseVisualStyleBackColor = true;
    // 
    // rtbLog
    // 
    rtbLog.BackColor = Color.Black;
    rtbLog.BorderStyle = BorderStyle.None;
    rtbLog.Dock = DockStyle.Fill;
    rtbLog.Font = new Font("Consolas", 9.5F);
    rtbLog.ForeColor = Color.Gainsboro;
    rtbLog.Location = new Point(3, 4);
    rtbLog.Margin = new Padding(3, 4, 3, 4);
    rtbLog.Name = "rtbLog";
    rtbLog.ReadOnly = true;
    rtbLog.Size = new Size(1423, 502);
    rtbLog.TabIndex = 0;
    rtbLog.Text = "";
    // 
    // tabErros
    // 
    tabErros.Controls.Add(lvErros);
    tabErros.Location = new Point(4, 29);
    tabErros.Margin = new Padding(3, 4, 3, 4);
    tabErros.Name = "tabErros";
    tabErros.Padding = new Padding(3, 4, 3, 4);
    tabErros.Size = new Size(1429, 510);
    tabErros.TabIndex = 1;
    tabErros.Text = "Erros";
    tabErros.UseVisualStyleBackColor = true;
    // 
    // lvErros
    // 
    lvErros.Columns.AddRange(new ColumnHeader[] { colOnda, colLinha, colArquivo, colMensagem });
    lvErros.Dock = DockStyle.Fill;
    lvErros.FullRowSelect = true;
    lvErros.GridLines = true;
    lvErros.Location = new Point(3, 4);
    lvErros.Margin = new Padding(3, 4, 3, 4);
    lvErros.Name = "lvErros";
    lvErros.Size = new Size(1423, 502);
    lvErros.TabIndex = 0;
    lvErros.UseCompatibleStateImageBehavior = false;
    lvErros.View = View.Details;
    // 
    // colOnda
    // 
    colOnda.Text = "Onda";
    // 
    // colLinha
    // 
    colLinha.Text = "Linha";
    // 
    // colArquivo
    // 
    colArquivo.Text = "Arquivo";
    colArquivo.Width = 320;
    // 
    // colMensagem
    // 
    colMensagem.Text = "Mensagem";
    colMensagem.Width = 500;
    // 
    // tabOrdem
    // 
    tabOrdem.Controls.Add(tvOrdem);
    tabOrdem.Controls.Add(lblDependencias);
    tabOrdem.Location = new Point(4, 29);
    tabOrdem.Margin = new Padding(3, 4, 3, 4);
    tabOrdem.Name = "tabOrdem";
    tabOrdem.Padding = new Padding(3, 4, 3, 4);
    tabOrdem.Size = new Size(1429, 510);
    tabOrdem.TabIndex = 2;
    tabOrdem.Text = "Ordem de build";
    tabOrdem.UseVisualStyleBackColor = true;
    // 
    // tvOrdem
    // 
    tvOrdem.Dock = DockStyle.Fill;
    tvOrdem.Location = new Point(3, 4);
    tvOrdem.Margin = new Padding(3, 4, 3, 4);
    tvOrdem.Name = "tvOrdem";
    tvOrdem.Size = new Size(1423, 449);
    tvOrdem.TabIndex = 0;
    // 
    // lblDependencias
    // 
    lblDependencias.Dock = DockStyle.Bottom;
    lblDependencias.Location = new Point(3, 453);
    lblDependencias.Name = "lblDependencias";
    lblDependencias.Size = new Size(1423, 53);
    lblDependencias.TabIndex = 1;
    // 
    // tabResumo
    // 
    tabResumo.Controls.Add(txtResumo);
    tabResumo.Controls.Add(btnCopiarResumo);
    tabResumo.Location = new Point(4, 29);
    tabResumo.Margin = new Padding(3, 4, 3, 4);
    tabResumo.Name = "tabResumo";
    tabResumo.Size = new Size(1429, 510);
    tabResumo.TabIndex = 3;
    tabResumo.Text = "Resumo";
    tabResumo.UseVisualStyleBackColor = true;
    // 
    // txtResumo
    // 
    txtResumo.Dock = DockStyle.Fill;
    txtResumo.Font = new Font("Consolas", 9.5F);
    txtResumo.Location = new Point(0, 0);
    txtResumo.Margin = new Padding(3, 4, 3, 4);
    txtResumo.Multiline = true;
    txtResumo.Name = "txtResumo";
    txtResumo.ReadOnly = true;
    txtResumo.ScrollBars = ScrollBars.Vertical;
    txtResumo.Size = new Size(1429, 470);
    txtResumo.TabIndex = 0;
    // 
    // btnCopiarResumo
    // 
    btnCopiarResumo.Dock = DockStyle.Bottom;
    btnCopiarResumo.Location = new Point(0, 470);
    btnCopiarResumo.Margin = new Padding(3, 4, 3, 4);
    btnCopiarResumo.Name = "btnCopiarResumo";
    btnCopiarResumo.Size = new Size(1429, 40);
    btnCopiarResumo.TabIndex = 1;
    btnCopiarResumo.Text = "Copiar resumo";
    btnCopiarResumo.UseVisualStyleBackColor = true;
    // 
    // timerProgresso
    // 
    timerProgresso.Interval = 1000;
    // 
    // MainForm
    // 
    AutoScaleDimensions = new SizeF(8F, 20F);
    AutoScaleMode = AutoScaleMode.Font;
    ClientSize = new Size(1437, 800);
    Controls.Add(tabControl);
    Controls.Add(painelProgresso);
    Controls.Add(painelBotoes);
    Controls.Add(painelTopo);
    Controls.Add(menuStrip);
    MainMenuStrip = menuStrip;
    Margin = new Padding(3, 4, 3, 4);
    MinimumSize = new Size(797, 584);
    Name = "MainForm";
    StartPosition = FormStartPosition.CenterScreen;
    Text = "GitSubmoduleSync";
    menuStrip.ResumeLayout(false);
    menuStrip.PerformLayout();
    painelTopo.ResumeLayout(false);
    painelTopo.PerformLayout();
    painelBotoes.ResumeLayout(false);
    painelProgresso.ResumeLayout(false);
    painelProgresso.PerformLayout();
    tabControl.ResumeLayout(false);
    tabLog.ResumeLayout(false);
    tabErros.ResumeLayout(false);
    tabOrdem.ResumeLayout(false);
    tabResumo.ResumeLayout(false);
    tabResumo.PerformLayout();
    ResumeLayout(false);
    PerformLayout();
  }

  #endregion
}
