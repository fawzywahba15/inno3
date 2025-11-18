using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace MoodleIndexer.Services;

public class CsvExtractor
{
    public async Task<string> ExtractFromUrl(string url)
    {
        try
        {
            Console.WriteLine($"[INFO] Lade CSV von URL: {url}");
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return ParseCsv(reader);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim CSV-Download: {ex.Message}");
            return "";
        }
    }

    public string ExtractFromLocalHash(string contentHash, string basePath)
    {
        try
        {
            var subfolder1 = contentHash[..2];
            var subfolder2 = contentHash[2..4];
            var fullPath = Path.Combine(basePath, subfolder1, subfolder2, contentHash);

            Console.WriteLine($"[INFO] Versuche lokale CSV zu laden: {fullPath}");

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[ERROR] Lokale CSV nicht gefunden: {fullPath}");
                return "";
            }

            using var reader = new StreamReader(fullPath, Encoding.UTF8);
            return ParseCsv(reader);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim lokalen CSV-Parsing: {ex.Message}");
            return "";
        }
    }

    private string ParseCsv(TextReader reader)
    {
        var result = new StringBuilder();
        using var parser = new TextFieldParser(reader)
        {
            Delimiters = new[] { "," },
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = true
        };

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields != null)
                result.AppendLine(string.Join(" | ", fields));
        }

        return result.ToString().Trim();
    }
    
    public string ExtractFromBytes(byte[] fileBytes)
    {
        try
        {
            Console.WriteLine($"[INFO] Starte In-Memory CSV-Parsing.");
            // 1. Erstelle einen MemoryStream aus dem Byte-Array
            using var stream = new MemoryStream(fileBytes);
            
            // 2. Erstelle einen StreamReader, um den Stream als Text zu lesen (wichtig: UTF8 verwenden)
            using var reader = new StreamReader(stream, Encoding.UTF8);
            
            // 3. Verwende die bestehende Parse-Logik
            return ParseCsv(reader);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim In-Memory CSV-Parsing: {ex.Message}");
            return "";
        }
    }
}
