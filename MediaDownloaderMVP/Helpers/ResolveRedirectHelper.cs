namespace MediaDownloaderTgBotMVP.Helpers;

public static class ResolveRedirectHelper
{
    public static async Task<string> ResolveRedirectAsync(string url, CancellationToken ct)
    {
        try
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = true };
            using var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);

            return response.RequestMessage?.RequestUri?.ToString() ?? url;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RedirectResolver] ⚠️ Не вдалося розгорнути посилання: {ex.Message}");
            return url;
        }
    }
}
