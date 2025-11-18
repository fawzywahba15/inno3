using DocumentFormat.OpenXml.Packaging;
using System.Text;

namespace MoodleIndexer.Services;

public class WordExtractor
{
    public async Task<string> ExtractFromUrl(string url)
    {
        try
        {
            Console.WriteLine($"[INFO] Starte DOCX-Download von URL: {url}");
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
            return ExtractTextFromStream(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim Word-Download: {ex.Message}");
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

            Console.WriteLine($"[INFO] Lokale DOCX-Datei prüfen: {fullPath}");

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[ERROR] Lokale Word-Datei nicht gefunden: {fullPath}");
                return "";
            }

            using var fileStream = File.OpenRead(fullPath);
            return ExtractTextFromStream(fileStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim lokalen Word-Parsing: {ex.Message}");
            return "";
        }
    }

    private string ExtractTextFromStream(Stream stream)
    {
        using var mem = new MemoryStream();
        stream.CopyTo(mem);
        mem.Position = 0;

        var sb = new StringBuilder();

        using var wordDoc = WordprocessingDocument.Open(mem, false);
        var body = wordDoc.MainDocumentPart?.Document?.Body;
        if (body != null)
        {
            sb.Append(body.InnerText);
        }

        return sb.ToString().Trim();
    }
}
