using System.Net.Http.Headers;
using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Services;

public class TransferService(HttpClient httpClient, SettingService settings) : ITransferService
{
    public async Task<bool> SendBinaryDataAsync(byte[] binaryData)
    {
        using var content = new ByteArrayContent(binaryData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await httpClient.PostAsync(settings.EndpointUrl, content);
        return response.IsSuccessStatusCode;
    }
}
