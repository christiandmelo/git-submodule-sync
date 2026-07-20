namespace GitSubmoduleSync.UI;

internal static class IconeApp
{
  private static readonly Icon? Instancia = CarregarIcone();

  public static Icon? Obter() => Instancia;

  private static Icon? CarregarIcone()
  {
    try
    {
      return Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
    }
    catch (Exception ex) when (ex is IOException or ArgumentException)
    {
      return null;
    }
  }
}
