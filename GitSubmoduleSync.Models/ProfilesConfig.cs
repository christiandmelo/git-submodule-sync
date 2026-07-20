using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitSubmoduleSync.Models;

public sealed class ProfilesConfig
{
  public string PerfilAtivo { get; set; } = "";
  public List<SyncProfile> Perfis { get; set; } = new();

  private static readonly JsonSerializerOptions JsonOpcoes = new()
  {
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
  };

  public static string CaminhoArquivo =>
    Path.Combine(AppContext.BaseDirectory, "configs.json");

  /// <summary>Motivo do último Carregar() ter voltado com instância vazia; null se carregou normalmente.</summary>
  public static string? UltimoErroCarregamento { get; private set; }

  public static ProfilesConfig Carregar()
  {
    UltimoErroCarregamento = null;

    if (!File.Exists(CaminhoArquivo))
    {
      return new ProfilesConfig();
    }

    try
    {
      var json = File.ReadAllText(CaminhoArquivo);
      var config = JsonSerializer.Deserialize<ProfilesConfig>(json, JsonOpcoes);
      return config ?? new ProfilesConfig();
    }
    catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
    {
      UltimoErroCarregamento = ex.Message;
      return new ProfilesConfig();
    }
  }

  public void Salvar()
  {
    var json = JsonSerializer.Serialize(this, JsonOpcoes);
    var tmp = CaminhoArquivo + ".tmp";
    File.WriteAllText(tmp, json);
    File.Move(tmp, CaminhoArquivo, overwrite: true);
  }

  public SyncProfile? ObterAtivo() =>
    Perfis.FirstOrDefault(p => p.Nome == PerfilAtivo);
}
