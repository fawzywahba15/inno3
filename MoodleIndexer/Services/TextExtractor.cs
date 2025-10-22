using System.Net.Http;
using System.Text;

namespace MoodleIndexer.Services;

public class TextExtractor
{
    public async Task<string> ExtractFromUrl(string url)
    {
        try
        {
            Console.WriteLine($"[INFO] Text-Datei wird von URL geladen: {url}");
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[DEBUG] Text aus URL geladen ({content.Length} Zeichen)");
            return content.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim Laden der Textdatei aus URL: {ex.Message}");
            return "";
        }
    }

    public string ExtractFromLocalHash(string contentHash, string basePath)
    {
        try
        {
            var sub1 = contentHash.Substring(0, 2);
            var sub2 = contentHash.Substring(2, 2);
            var fullPath = Path.Combine(basePath, sub1, sub2, contentHash);

            Console.WriteLine($"[INFO] Text-Datei lokal laden: {fullPath}");

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[WARN] Lokale Textdatei nicht gefunden: {fullPath}");
                return "";
            }

            var text = File.ReadAllText(fullPath, Encoding.UTF8);
            Console.WriteLine($"[DEBUG] Lokaler Text geladen ({text.Length} Zeichen)");
            return text.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim lokalen Lesen: {ex.Message}");
            return "";
        }
    }
}