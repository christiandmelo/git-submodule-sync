namespace GitSubmoduleSync.UI;

/// <summary>Diálogo simples de texto único — evita a dependência do Microsoft.VisualBasic.Interaction.</summary>
public partial class PromptForm : Form
{
  public string Valor => txtValor.Text.Trim();

  public PromptForm() : this("", "")
  {
  }

  public PromptForm(string titulo, string rotulo, string valorInicial = "")
  {
    InitializeComponent();
    Text = titulo;
    lblRotulo.Text = rotulo;
    txtValor.Text = valorInicial;
  }
}
