using Newtonsoft.Json.Linq;

namespace BrickPrinterApp.Interfaces;

public interface IWotService
{
    Task<JObject?> GetPlayerSessionsAsync(string playerId);
    Task<JObject?> GetPlayerDataAsync(string playerId);
}