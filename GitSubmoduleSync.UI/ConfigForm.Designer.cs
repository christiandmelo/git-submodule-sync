namespace GitSubmoduleSync.UI;

partial class ConfigForm
{
  private System.ComponentModel.IContainer components = null;

  protected override void Dispose(bool disposing)
  {
    if (disposing && (components != null)) components.Dispose();
    base.Dispose(disposing);
  }

  #region Windows Form Designer generated code

  private System.Windows.Forms.ComboBox cboPerfil;
  private System.Windows.Forms.Button btnNovo;
  private System.Windows.Forms.Button btnDuplicar;
  private System.Windows.Forms.Button btnExcluir;

  private System.Windows.Forms.Label lblNome;
  private System.Windows.Forms.TextBox txtNome;
  private System.Windows.Forms.Label lblPastaRaiz;
  private System.Windows.Forms.TextBox txtPastaRaiz;
  private System.Windows.Forms.Button btnProcurar;
  private System.Windows.Forms.Label lblBranchBase;
  private System.Windows.Forms.TextBox txtBranchBase;
  private System.Windows.Forms.Label lblAvisoPasta;

  private System.Windows.Forms.Button btnLerProjetos;
  private System.Windows.Forms.DataGridView grid;
  private System.Windows.Forms.Button btnRestaurarTodos;

  private System.Windows.Forms.CheckBox chkAtualizarPai;
  private System.Windows.Forms.CheckBox chkIgnorarTestes;
  private System.Windows.Forms.CheckBox chkBuildIncremental;
  private System.Windows.Forms.Label lblParalelismoGit;
  private System.Windows.Forms.NumericUpDown numParalelismoGit;
  private System.Windows.Forms.Label lblParalelismoBuild;
  private System.Windows.Forms.NumericUpDown numParalelismoBuild;

  private System.Windows.Forms.Button btnSalvar;
  private System.Windows.Forms.Button btnCancelar;

  private void InitializeComponent()
  {
    cboPerfil = new ComboBox();
    btnNovo = new Button();
    btnDuplicar = new Button();
    btnExcluir = new Button();
    lblNome = new Label();
    txtNome = new TextBox();
    lblPastaRaiz = new Label();
    txtPastaRaiz = new TextBox();
    btnProcurar = new Button();
    lblAvisoPasta = new Label();
    lblBranchBase = new Label();
    txtBranchBase = new TextBox();
    btnLerProjetos = new Button();
    grid = new DataGridView();
    btnRestaurarTodos = new Button();
    chkAtualizarPai = new CheckBox();
    chkIgnorarTestes = new CheckBox();
    chkBuildIncremental = new CheckBox();
    lblParalelismoGit = new Label();
    numParalelismoGit = new NumericUpDown();
    lblParalelismoBuild = new Label();
    numParalelismoBuild = new NumericUpDown();
    btnSalvar = new Button();
    btnCancelar = new Button();
    ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
    ((System.ComponentModel.ISupportInitialize)numParalelismoGit).BeginInit();
    ((System.ComponentModel.ISupportInitialize)numParalelismoBuild).BeginInit();
    SuspendLayout();
    // 
    // cboPerfil
    // 
    cboPerfil.DropDownStyle = ComboBoxStyle.DropDownList;
    cboPerfil.Location = new Point(14, 16);
    cboPerfil.Margin = new Padding(3, 4, 3, 4);
    cboPerfil.Name = "cboPerfil";
    cboPerfil.Size = new Size(228, 28);
    cboPerfil.TabIndex = 0;
    // 
    // btnNovo
    // 
    btnNovo.Location = new Point(251, 15);
    btnNovo.Margin = new Padding(3, 4, 3, 4);
    btnNovo.Name = "btnNovo";
    btnNovo.Size = new Size(80, 29);
    btnNovo.TabIndex = 1;
    btnNovo.Text = "Novo";
    btnNovo.UseVisualStyleBackColor = true;
    // 
    // btnDuplicar
    // 
    btnDuplicar.Location = new Point(338, 15);
    btnDuplicar.Margin = new Padding(3, 4, 3, 4);
    btnDuplicar.Name = "btnDuplicar";
    btnDuplicar.Size = new Size(80, 29);
    btnDuplicar.TabIndex = 2;
    btnDuplicar.Text = "Duplicar";
    btnDuplicar.UseVisualStyleBackColor = true;
    // 
    // btnExcluir
    // 
    btnExcluir.Location = new Point(425, 15);
    btnExcluir.Margin = new Padding(3, 4, 3, 4);
    btnExcluir.Name = "btnExcluir";
    btnExcluir.Size = new Size(80, 29);
    btnExcluir.TabIndex = 3;
    btnExcluir.Text = "Excluir";
    btnExcluir.UseVisualStyleBackColor = true;
    // 
    // lblNome
    // 
    lblNome.AutoSize = true;
    lblNome.Location = new Point(14, 61);
    lblNome.Name = "lblNome";
    lblNome.Size = new Size(114, 20);
    lblNome.TabIndex = 4;
    lblNome.Text = "Nome do perfil:";
    // 
    // txtNome
    // 
    txtNome.Location = new Point(160, 57);
    txtNome.Margin = new Padding(3, 4, 3, 4);
    txtNome.Name = "txtNome";
    txtNome.Size = new Size(342, 27);
    txtNome.TabIndex = 5;
    // 
    // lblPastaRaiz
    // 
    lblPastaRaiz.AutoSize = true;
    lblPastaRaiz.Location = new Point(14, 99);
    lblPastaRaiz.Name = "lblPastaRaiz";
    lblPastaRaiz.Size = new Size(74, 20);
    lblPastaRaiz.TabIndex = 6;
    lblPastaRaiz.Text = "Pasta raiz:";
    // 
    // txtPastaRaiz
    // 
    txtPastaRaiz.Location = new Point(160, 95);
    txtPastaRaiz.Margin = new Padding(3, 4, 3, 4);
    txtPastaRaiz.Name = "txtPastaRaiz";
    txtPastaRaiz.Size = new Size(571, 27);
    txtPastaRaiz.TabIndex = 7;
    // 
    // btnProcurar
    // 
    btnProcurar.Location = new Point(738, 93);
    btnProcurar.Margin = new Padding(3, 4, 3, 4);
    btnProcurar.Name = "btnProcurar";
    btnProcurar.Size = new Size(103, 29);
    btnProcurar.TabIndex = 8;
    btnProcurar.Text = "Procurar…";
    btnProcurar.UseVisualStyleBackColor = true;
    // 
    // lblAvisoPasta
    // 
    lblAvisoPasta.AutoSize = true;
    lblAvisoPasta.ForeColor = Color.DarkOrange;
    lblAvisoPasta.Location = new Point(160, 125);
    lblAvisoPasta.Name = "lblAvisoPasta";
    lblAvisoPasta.Size = new Size(0, 20);
    lblAvisoPasta.TabIndex = 9;
    // 
    // lblBranchBase
    // 
    lblBranchBase.AutoSize = true;
    lblBranchBase.Location = new Point(14, 134);
    lblBranchBase.Name = "lblBranchBase";
    lblBranchBase.Size = new Size(92, 20);
    lblBranchBase.TabIndex = 10;
    lblBranchBase.Text = "Branch base:";
    // 
    // txtBranchBase
    // 
    txtBranchBase.Location = new Point(160, 130);
    txtBranchBase.Margin = new Padding(3, 4, 3, 4);
    txtBranchBase.Name = "txtBranchBase";
    txtBranchBase.Size = new Size(228, 27);
    txtBranchBase.TabIndex = 11;
    // 
    // btnLerProjetos
    // 
    btnLerProjetos.Location = new Point(738, 130);
    btnLerProjetos.Margin = new Padding(3, 4, 3, 4);
    btnLerProjetos.Name = "btnLerProjetos";
    btnLerProjetos.Size = new Size(103, 33);
    btnLerProjetos.TabIndex = 12;
    btnLerProjetos.Text = "Ler projetos";
    btnLerProjetos.UseVisualStyleBackColor = true;
    // 
    // grid
    // 
    grid.AllowUserToAddRows = false;
    grid.AllowUserToDeleteRows = false;
    grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    grid.ColumnHeadersHeight = 29;
    grid.Location = new Point(16, 171);
    grid.Margin = new Padding(3, 4, 3, 4);
    grid.Name = "grid";
    grid.RowHeadersVisible = false;
    grid.RowHeadersWidth = 51;
    grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    grid.Size = new Size(827, 347);
    grid.TabIndex = 13;
    // 
    // btnRestaurarTodos
    // 
    btnRestaurarTodos.Location = new Point(16, 525);
    btnRestaurarTodos.Margin = new Padding(3, 4, 3, 4);
    btnRestaurarTodos.Name = "btnRestaurarTodos";
    btnRestaurarTodos.Size = new Size(251, 33);
    btnRestaurarTodos.TabIndex = 14;
    btnRestaurarTodos.Text = "Restaurar todos para a base";
    btnRestaurarTodos.UseVisualStyleBackColor = true;
    // 
    // chkAtualizarPai
    // 
    chkAtualizarPai.AutoSize = true;
    chkAtualizarPai.Location = new Point(16, 571);
    chkAtualizarPai.Margin = new Padding(3, 4, 3, 4);
    chkAtualizarPai.Name = "chkAtualizarPai";
    chkAtualizarPai.Size = new Size(192, 24);
    chkAtualizarPai.TabIndex = 15;
    chkAtualizarPai.Text = "Atualizar repositório pai";
    chkAtualizarPai.UseVisualStyleBackColor = true;
    // 
    // chkIgnorarTestes
    // 
    chkIgnorarTestes.AutoSize = true;
    chkIgnorarTestes.Location = new Point(253, 571);
    chkIgnorarTestes.Margin = new Padding(3, 4, 3, 4);
    chkIgnorarTestes.Name = "chkIgnorarTestes";
    chkIgnorarTestes.Size = new Size(195, 24);
    chkIgnorarTestes.TabIndex = 16;
    chkIgnorarTestes.Text = "Ignorar projetos de teste";
    chkIgnorarTestes.UseVisualStyleBackColor = true;
    // 
    // chkBuildIncremental
    // 
    chkBuildIncremental.AutoSize = true;
    chkBuildIncremental.Location = new Point(454, 571);
    chkBuildIncremental.Margin = new Padding(3, 4, 3, 4);
    chkBuildIncremental.Name = "chkBuildIncremental";
    chkBuildIncremental.Size = new Size(147, 24);
    chkBuildIncremental.TabIndex = 17;
    chkBuildIncremental.Text = "Build incremental";
    chkBuildIncremental.UseVisualStyleBackColor = true;
    // 
    // lblParalelismoGit
    // 
    lblParalelismoGit.AutoSize = true;
    lblParalelismoGit.Location = new Point(14, 609);
    lblParalelismoGit.Name = "lblParalelismoGit";
    lblParalelismoGit.Size = new Size(110, 20);
    lblParalelismoGit.TabIndex = 18;
    lblParalelismoGit.Text = "Paralelismo git:";
    // 
    // numParalelismoGit
    // 
    numParalelismoGit.Location = new Point(160, 605);
    numParalelismoGit.Margin = new Padding(3, 4, 3, 4);
    numParalelismoGit.Maximum = new decimal(new int[] { 16, 0, 0, 0 });
    numParalelismoGit.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
    numParalelismoGit.Name = "numParalelismoGit";
    numParalelismoGit.Size = new Size(69, 27);
    numParalelismoGit.TabIndex = 19;
    numParalelismoGit.Value = new decimal(new int[] { 4, 0, 0, 0 });
    // 
    // lblParalelismoBuild
    // 
    lblParalelismoBuild.AutoSize = true;
    lblParalelismoBuild.Location = new Point(251, 609);
    lblParalelismoBuild.Name = "lblParalelismoBuild";
    lblParalelismoBuild.Size = new Size(196, 20);
    lblParalelismoBuild.TabIndex = 20;
    lblParalelismoBuild.Text = "Paralelismo build (0 = auto):";
    // 
    // numParalelismoBuild
    // 
    numParalelismoBuild.Location = new Point(480, 605);
    numParalelismoBuild.Margin = new Padding(3, 4, 3, 4);
    numParalelismoBuild.Maximum = new decimal(new int[] { 64, 0, 0, 0 });
    numParalelismoBuild.Name = "numParalelismoBuild";
    numParalelismoBuild.Size = new Size(69, 27);
    numParalelismoBuild.TabIndex = 21;
    // 
    // btnSalvar
    // 
    btnSalvar.Location = new Point(663, 605);
    btnSalvar.Margin = new Padding(3, 4, 3, 4);
    btnSalvar.Name = "btnSalvar";
    btnSalvar.Size = new Size(86, 31);
    btnSalvar.TabIndex = 22;
    btnSalvar.Text = "Salvar";
    btnSalvar.UseVisualStyleBackColor = true;
    // 
    // btnCancelar
    // 
    btnCancelar.DialogResult = DialogResult.Cancel;
    btnCancelar.Location = new Point(755, 605);
    btnCancelar.Margin = new Padding(3, 4, 3, 4);
    btnCancelar.Name = "btnCancelar";
    btnCancelar.Size = new Size(86, 31);
    btnCancelar.TabIndex = 23;
    btnCancelar.Text = "Cancelar";
    btnCancelar.UseVisualStyleBackColor = true;
    // 
    // ConfigForm
    // 
    AutoScaleDimensions = new SizeF(8F, 20F);
    AutoScaleMode = AutoScaleMode.Font;
    CancelButton = btnCancelar;
    ClientSize = new Size(855, 649);
    Controls.Add(cboPerfil);
    Controls.Add(btnNovo);
    Controls.Add(btnDuplicar);
    Controls.Add(btnExcluir);
    Controls.Add(lblNome);
    Controls.Add(txtNome);
    Controls.Add(lblPastaRaiz);
    Controls.Add(txtPastaRaiz);
    Controls.Add(btnProcurar);
    Controls.Add(lblAvisoPasta);
    Controls.Add(lblBranchBase);
    Controls.Add(txtBranchBase);
    Controls.Add(btnLerProjetos);
    Controls.Add(grid);
    Controls.Add(btnRestaurarTodos);
    Controls.Add(chkAtualizarPai);
    Controls.Add(chkIgnorarTestes);
    Controls.Add(chkBuildIncremental);
    Controls.Add(lblParalelismoGit);
    Controls.Add(numParalelismoGit);
    Controls.Add(lblParalelismoBuild);
    Controls.Add(numParalelismoBuild);
    Controls.Add(btnSalvar);
    Controls.Add(btnCancelar);
    FormBorderStyle = FormBorderStyle.FixedDialog;
    Margin = new Padding(3, 4, 3, 4);
    MaximizeBox = false;
    MinimizeBox = false;
    Name = "ConfigForm";
    StartPosition = FormStartPosition.CenterParent;
    Text = "Configurações — GitSubmoduleSync";
    ((System.ComponentModel.ISupportInitialize)grid).EndInit();
    ((System.ComponentModel.ISupportInitialize)numParalelismoGit).EndInit();
    ((System.ComponentModel.ISupportInitialize)numParalelismoBuild).EndInit();
    ResumeLayout(false);
    PerformLayout();
  }

  #endregion
}
