using System.Text.Json;

namespace GitSubmoduleSync.Models;

public static class CaminhoConfig
{
  private const string AppId = "git-submodule-sync";
  private const string PropriedadeEsperada = "Perfis";

  public static string Pasta => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "apps-cm", AppId);

  public static string Arquivo => Path.Combine(Pasta, "configs.json");

  /// <summary>Migra uma única vez o configs.json legado que ficava ao lado do exe.</summary>
  public static void MigrarSeNecessario()
  {
    Directory.CreateDirectory(Pasta);
    if (File.Exists(Arquivo)) return;

    var legado = Path.Combine(AppContext.BaseDirectory, "configs.json");
    if (!File.Exists(legado)) return;

    try
    {
      // TestCoverageUI e GitSubmoduleSync usavam o mesmo nome "configs.json" no mesmo diretório
      // (a pasta do exe); checar o schema antes de adotar evita importar a config do app errado.
      using var doc = JsonDocument.Parse(File.ReadAllText(legado));
      if (!doc.RootElement.TryGetProperty(PropriedadeEsperada, out _)) return;

      File.Copy(legado, Arquivo); // copiar, não mover: o legado continua disponível para rollback
    }
    catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
    {
      // Migração é best-effort; seguir com config vazia é aceitável.
    }
  }
}
