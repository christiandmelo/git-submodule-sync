namespace GitSubmoduleSync.UI;

/// <summary>Diálogo simples de texto único — evita a dependência do Microsoft.VisualBasic.Interaction.</summary>
public sealed class PromptForm : Form
{
  private readonly TextBox _textBox;

  public string Valor => _textBox.Text.Trim();

  public PromptForm(string titulo, string rotulo, string valorInicial = "")
  {
    Text = titulo;
    FormBorderStyle = FormBorderStyle.FixedDialog;
    StartPosition = FormStartPosition.CenterParent;
    MinimizeBox = false;
    MaximizeBox = false;
    ClientSize = new Size(360, 110);

    var lbl = new Label { Text = rotulo, Location = new Point(12, 12), AutoSize = true };
    _textBox = new TextBox { Text = valorInicial, Location = new Point(12, 34), Width = 336 };
    var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(190, 68), Width = 75 };
    var btnCancelar = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(273, 68), Width = 75 };

    Controls.AddRange(new Control[] { lbl, _textBox, btnOk, btnCancelar });
    AcceptButton = btnOk;
    CancelButton = btnCancelar;
  }
}
