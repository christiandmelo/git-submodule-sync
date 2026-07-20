namespace GitSubmoduleSync.UI;

partial class PromptForm
{
  private System.ComponentModel.IContainer components = null;

  protected override void Dispose(bool disposing)
  {
    if (disposing && (components != null)) components.Dispose();
    base.Dispose(disposing);
  }

  #region Windows Form Designer generated code

  private System.Windows.Forms.Label lblRotulo;
  private System.Windows.Forms.TextBox txtValor;
  private System.Windows.Forms.Button btnOk;
  private System.Windows.Forms.Button btnCancelar;

  private void InitializeComponent()
  {
    this.lblRotulo = new System.Windows.Forms.Label();
    this.txtValor = new System.Windows.Forms.TextBox();
    this.btnOk = new System.Windows.Forms.Button();
    this.btnCancelar = new System.Windows.Forms.Button();
    this.SuspendLayout();
    //
    // lblRotulo
    //
    this.lblRotulo.AutoSize = true;
    this.lblRotulo.Location = new System.Drawing.Point(12, 12);
    this.lblRotulo.Name = "lblRotulo";
    this.lblRotulo.Size = new System.Drawing.Size(0, 15);
    this.lblRotulo.TabIndex = 0;
    //
    // txtValor
    //
    this.txtValor.Location = new System.Drawing.Point(12, 34);
    this.txtValor.Name = "txtValor";
    this.txtValor.Size = new System.Drawing.Size(336, 23);
    this.txtValor.TabIndex = 1;
    //
    // btnOk
    //
    this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
    this.btnOk.Location = new System.Drawing.Point(190, 68);
    this.btnOk.Name = "btnOk";
    this.btnOk.Size = new System.Drawing.Size(75, 25);
    this.btnOk.TabIndex = 2;
    this.btnOk.Text = "OK";
    this.btnOk.UseVisualStyleBackColor = true;
    //
    // btnCancelar
    //
    this.btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel;
    this.btnCancelar.Location = new System.Drawing.Point(273, 68);
    this.btnCancelar.Name = "btnCancelar";
    this.btnCancelar.Size = new System.Drawing.Size(75, 25);
    this.btnCancelar.TabIndex = 3;
    this.btnCancelar.Text = "Cancelar";
    this.btnCancelar.UseVisualStyleBackColor = true;
    //
    // PromptForm
    //
    this.AcceptButton = this.btnOk;
    this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
    this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
    this.CancelButton = this.btnCancelar;
    this.ClientSize = new System.Drawing.Size(360, 110);
    this.Controls.Add(this.lblRotulo);
    this.Controls.Add(this.txtValor);
    this.Controls.Add(this.btnOk);
    this.Controls.Add(this.btnCancelar);
    this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
    this.MaximizeBox = false;
    this.MinimizeBox = false;
    this.Name = "PromptForm";
    this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
    this.ResumeLayout(false);
    this.PerformLayout();
  }

  #endregion
}
