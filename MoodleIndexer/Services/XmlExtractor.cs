using System.Xml;
using System.Xml.Linq;

namespace MoodleIndexer.Services;

public class XmlExtractor
{
    public async Task<string> ExtractFromUrl(string url)
    {
        try
        {
            Console.WriteLine($"[INFO] Lade XML-Datei von URL: {url}");
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var xml = await response.Content.ReadAsStringAsync();
            return FormatXml(xml);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] XML von URL fehlgeschlagen: {ex.Message}");
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

            Console.WriteLine($"[INFO] Lade lokale XML-Datei: {path}");
            if (!File.Exists(path))
                return "";

            var xml = File.ReadAllText(path);
            return FormatXml(xml);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Lokales XML fehlgeschlagen: {ex.Message}");
            return "";
        }
    }

    private string FormatXml(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            return doc.ToString();
        }
        catch
        {
            return xml; // Rückfall auf Rohinhalt
        }
    }
    
    public string ExtractFromBytes(byte[] fileBytes)
    {
        try
        {
            Console.WriteLine($"[INFO] Starte In-Memory XML-Parsing.");
            
            // Konvertiere das Byte-Array in einen String (UTF8 ist der Standard für moderne XML/Web-Daten)
            var xml = System.Text.Encoding.UTF8.GetString(fileBytes);
            
            // Verwende die bestehende Formatierungslogik
            return FormatXml(xml);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim In-Memory XML-Parsing: {ex.Message}");
            return "";
        }
    }
    
}