using Newtonsoft.Json.Linq;
using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Services;

public class WotService : IWotService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.tomato.gg";

    public WotService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<JObject?> GetPlayerSessionsAsync(string playerId)
    {
        try
        {
            var url = $"{BaseUrl}/api/player/combined-battles/{playerId}?page=0&days=36500&pageSize=10&sortBy=battle_time&sortDirection=desc&platoon=in-and-outside-platoon&spawn=all&won=all&tankType=all";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var jobject = JObject.Parse(jsonString);
            return jobject;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Abrufen der WoT-Daten: {ex.Message}");
            return null;
        }
    }
    
    public async Task<JObject?> GetPlayerDataAsync(string playerId)
    {
        try
        {
            var url = $"{BaseUrl}/api/player/recents/eu/{playerId}?days=1,3,7,30,60&battles=1000,100&cache=false";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var jobject = JObject.Parse(jsonString);
            return jobject;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Abrufen der WoT-Daten: {ex.Message}");
            return null;
        }
    }
}