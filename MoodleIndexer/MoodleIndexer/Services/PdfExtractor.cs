using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace MoodleIndexer.Services;

public class PdfExtractor
{
    public async Task<string> ExtractFromUrl(string url)
    {
        try
        {
            Console.WriteLine($"[INFO] Starte PDF-Download von URL: {url}");
            using var client = new HttpClient();
            var response = await client.GetAsync(url);

            Console.WriteLine($"[DEBUG] HTTP Status: {response.StatusCode}");
            Console.WriteLine($"[DEBUG] Content-Type: {response.Content.Headers.ContentType}");
            Console.WriteLine($"[DEBUG] Content-Length: {response.Content.Headers.ContentLength ?? -1} Bytes");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[ERROR] HTTP-Fehler: {response.StatusCode}");
                return "";
            }

            await using var stream = await response.Content.ReadAsStreamAsync();

            // Sicherheitscheck: prüfen, ob es wirklich ein PDF ist
            byte[] header = new byte[5];
            await stream.ReadAsync(header, 0, 5);
            stream.Position = 0;

            var headerStr = System.Text.Encoding.ASCII.GetString(header);
            if (!headerStr.StartsWith("%PDF-"))
            {
                Console.WriteLine("[ERROR] Datei ist kein gültiges PDF (Header fehlt)");
                return "";
            }

            using var doc = PdfDocument.Open(stream);
            var result = ExtractTextFromDocument(doc);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim PDF-Download von {url}: {ex.Message}");
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

            Console.WriteLine($"[INFO] Lokale PDF-Datei prüfen: {fullPath}");

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[ERROR] Lokale Datei nicht gefunden: {fullPath}");
                return "";
            }

            using var doc = PdfDocument.Open(fullPath);
            var text = ExtractTextFromDocument(doc);

            Console.WriteLine($"[INFO] PDF erfolgreich geladen. Textlänge: {text.Length} Zeichen");
            return text.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim Lesen lokaler PDF-Datei: {ex.Message}");
            return "";
        }
    }

    private string ExtractTextFromDocument(PdfDocument document)
    {
        var result = new List<string>();
        foreach (Page page in document.GetPages())
        {
            result.Add(page.Text);
        }

        return string.Join("\n", result);
    }
}


/*using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace MoodleIndexer.Services;

public class PdfExtractor
{
    public async Task<string> ExtractFromUrl(string url)
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = PdfDocument.Open(stream);
            return string.Join("\n", doc.GetPages().Select(p => p.Text));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PDF] Fehler beim Laden aus URL {url}: {ex.Message}");
            return "";
        }
    }

    public string ExtractFromLocalHash(string contentHash, string basePath)
    {
        try
        {
            var subfolder1 = contentHash.Substring(0, 2);
            var subfolder2 = contentHash.Substring(2, 2);
            var fullPath = Path.Combine(basePath, subfolder1, subfolder2, contentHash);

            Console.WriteLine($"📂 Versuche lokale PDF zu laden: {fullPath}");

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"❌ Lokale Datei nicht gefunden: {fullPath}");
                return "";
            }

            using var doc = PdfDocument.Open(fullPath);
            var text = string.Join("\n", doc.GetPages().Select(p => p.Text));
            Console.WriteLine($"📄 Extrahierter Text (erste 300 Zeichen): {text.Substring(0, Math.Min(300, text.Length))}");
            return text.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fehler beim lokalen PDF-Parsing ({contentHash}): {ex.Message}");
            return "";
        }
    }
}*/