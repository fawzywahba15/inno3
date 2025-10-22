using System.Text.Json;

namespace MoodleIndexer.Services;

public class JsonExtractor
{
    public async Task<string> ExtractFromUrl(string url)
    {
        try
        {
            Console.WriteLine($"[INFO] Lade JSON-Datei von URL: {url}");
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return FormatJson(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] JSON von URL fehlgeschlagen: {ex.Message}");
            return "";
        }
    }

    public string ExtractFromLocalHash(string contentHash, string basePath)
    {
        try
        {
            var subfolder1 = contentHash[..2];
            var subfolder2 = contentHash[2..4];
            var path = Path.Combine(basePath, subfolder1, subfolder2, contentHash);

            Console.WriteLine($"[INFO] Lade lokale JSON-Datei: {path}");
            if (!File.Exists(path))
                return "";

            var json = File.ReadAllText(path);
            return FormatJson(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Lokales JSON fehlgeschlagen: {ex.Message}");
            return "";
        }
    }

    private string FormatJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json; // Rückfall auf Rohinhalt, falls ungültig
        }
    }
}