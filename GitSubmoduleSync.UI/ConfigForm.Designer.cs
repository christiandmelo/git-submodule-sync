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
    cboPerfil.Location = new Point(0, 0);
    cboPerfil.Name = "cboPerfil";
    cboPerfil.Size = new Size(121, 28);
    cboPerfil.TabIndex = 0;
    // 
    // btnNovo
    // 
    btnNovo.Location = new Point(0, 0);
    btnNovo.Name = "btnNovo";
    btnNovo.Size = new Size(75, 23);
    btnNovo.TabIndex = 1;
    // 
    // btnDuplicar
    // 
    btnDuplicar.Location = new Point(0, 0);
    btnDuplicar.Name = "btnDuplicar";
    btnDuplicar.Size = new Size(75, 23);
    btnDuplicar.TabIndex = 2;
    // 
    // btnExcluir
    // 
    btnExcluir.Location = new Point(0, 0);
    btnExcluir.Name = "btnExcluir";
    btnExcluir.Size = new Size(75, 23);
    btnExcluir.TabIndex = 3;
    // 
    // lblNome
    // 
    lblNome.Location = new Point(0, 0);
    lblNome.Name = "lblNome";
    lblNome.Size = new Size(100, 23);
    lblNome.TabIndex = 4;
    // 
    // txtNome
    // 
    txtNome.Location = new Point(0, 0);
    txtNome.Name = "txtNome";
    txtNome.Size = new Size(100, 27);
    txtNome.TabIndex = 5;
    // 
    // lblPastaRaiz
    // 
    lblPastaRaiz.Location = new Point(0, 0);
    lblPastaRaiz.Name = "lblPastaRaiz";
    lblPastaRaiz.Size = new Size(100, 23);
    lblPastaRaiz.TabIndex = 6;
    // 
    // txtPastaRaiz
    // 
    txtPastaRaiz.Location = new Point(0, 0);
    txtPastaRaiz.Name = "txtPastaRaiz";
    txtPastaRaiz.Size = new Size(100, 27);
    txtPastaRaiz.TabIndex = 7;
    // 
    // btnProcurar
    // 
    btnProcurar.Location = new Point(0, 0);
    btnProcurar.Name = "btnProcurar";
    btnProcurar.Size = new Size(75, 23);
    btnProcurar.TabIndex = 8;
    // 
    // lblAvisoPasta
    // 
    lblAvisoPasta.Location = new Point(0, 0);
    lblAvisoPasta.Name = "lblAvisoPasta";
    lblAvisoPasta.Size = new Size(100, 23);
    lblAvisoPasta.TabIndex = 9;
    // 
    // lblBranchBase
    // 
    lblBranchBase.Location = new Point(0, 0);
    lblBranchBase.Name = "lblBranchBase";
    lblBranchBase.Size = new Size(100, 23);
    lblBranchBase.TabIndex = 10;
    // 
    // txtBranchBase
    // 
    txtBranchBase.Location = new Point(0, 0);
    txtBranchBase.Name = "txtBranchBase";
    txtBranchBase.Size = new Size(100, 27);
    txtBranchBase.TabIndex = 11;
    // 
    // btnLerProjetos
    // 
    btnLerProjetos.Location = new Point(0, 0);
    btnLerProjetos.Name = "btnLerProjetos";
    btnLerProjetos.Size = new Size(75, 23);
    btnLerProjetos.TabIndex = 12;
    // 
    // grid
    // 
    grid.ColumnHeadersHeight = 29;
    grid.Location = new Point(0, 0);
    grid.Name = "grid";
    grid.RowHeadersWidth = 51;
    grid.Size = new Size(240, 150);
    grid.TabIndex = 13;
    // 
    // btnRestaurarTodos
    // 
    btnRestaurarTodos.Location = new Point(0, 0);
    btnRestaurarTodos.Name = "btnRestaurarTodos";
    btnRestaurarTodos.Size = new Size(75, 23);
    btnRestaurarTodos.TabIndex = 14;
    // 
    // chkAtualizarPai
    // 
    chkAtualizarPai.Location = new Point(0, 0);
    chkAtualizarPai.Name = "chkAtualizarPai";
    chkAtualizarPai.Size = new Size(104, 24);
    chkAtualizarPai.TabIndex = 15;
    // 
    // chkIgnorarTestes
    // 
    chkIgnorarTestes.Location = new Point(0, 0);
    chkIgnorarTestes.Name = "chkIgnorarTestes";
    chkIgnorarTestes.Size = new Size(104, 24);
    chkIgnorarTestes.TabIndex = 16;
    // 
    // chkBuildIncremental
    // 
    chkBuildIncremental.Location = new Point(0, 0);
    chkBuildIncremental.Name = "chkBuildIncremental";
    chkBuildIncremental.Size = new Size(104, 24);
    chkBuildIncremental.TabIndex = 17;
    // 
    // lblParalelismoGit
    // 
    lblParalelismoGit.Location = new Point(0, 0);
    lblParalelismoGit.Name = "lblParalelismoGit";
    lblParalelismoGit.Size = new Size(100, 23);
    lblParalelismoGit.TabIndex = 18;
    // 
    // numParalelismoGit
    // 
    numParalelismoGit.Location = new Point(0, 0);
    numParalelismoGit.Name = "numParalelismoGit";
    numParalelismoGit.Size = new Size(120, 27);
    numParalelismoGit.TabIndex = 19;
    // 
    // lblParalelismoBuild
    // 
    lblParalelismoBuild.Location = new Point(0, 0);
    lblParalelismoBuild.Name = "lblParalelismoBuild";
    lblParalelismoBuild.Size = new Size(100, 23);
    lblParalelismoBuild.TabIndex = 20;
    // 
    // numParalelismoBuild
    // 
    numParalelismoBuild.Location = new Point(0, 0);
    numParalelismoBuild.Name = "numParalelismoBuild";
    numParalelismoBuild.Size = new Size(120, 27);
    numParalelismoBuild.TabIndex = 21;
    // 
    // btnSalvar
    // 
    btnSalvar.Location = new Point(0, 0);
    btnSalvar.Name = "btnSalvar";
    btnSalvar.Size = new Size(75, 23);
    btnSalvar.TabIndex = 22;
    // 
    // btnCancelar
    // 
    btnCancelar.Location = new Point(0, 0);
    btnCancelar.Name = "btnCancelar";
    btnCancelar.Size = new Size(75, 23);
    btnCancelar.TabIndex = 23;
    // 
    // ConfigForm
    // 
    AutoScaleDimensions = new SizeF(8F, 20F);
    AutoScaleMode = AutoScaleMode.Font;
    CancelButton = btnCancelar;
    ClientSize = new Size(748, 592);
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
