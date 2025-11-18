using System.Net;
using HtmlAgilityPack;

namespace MoodleIndexer.Services;

public class HtmlExtractor
{
    public async Task<string> ExtractFromUrl(string url)
    {
        try
        {
            using var client = new HttpClient();
            var html = await client.GetStringAsync(url);
            return ExtractText(html);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim Laden von HTML-URL: {ex.Message}");
            return "";
        }
    }

    public string ExtractFromLocalHash(string hash, string moodleDataDir)
    {
        try
        {
            var path = Path.Combine(
                moodleDataDir,
                hash.Substring(0, 2),
                hash.Substring(2, 2),
                hash
            );

            if (!File.Exists(path)) return "";

            var html = File.ReadAllText(path);
            return ExtractText(html);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim lokalen HTML-Parsing: {ex.Message}");
            return "";
        }
    }

    private string ExtractText(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return WebUtility.HtmlDecode(doc.DocumentNode.InnerText.Trim());
    }
    public string ExtractFromBytes(byte[] fileBytes)
    {
        try
        {
            Console.WriteLine($"[INFO] Starte In-Memory HTML-Parsing.");
            
            // Konvertiere das Byte-Array in einen String (UTF8 ist Standard für HTML)
            var html = System.Text.Encoding.UTF8.GetString(fileBytes);
            
            // Verwende die bestehende Extraktionslogik
            return ExtractText(html);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim In-Memory HTML-Parsing: {ex.Message}");
            return "";
        }
    }
}