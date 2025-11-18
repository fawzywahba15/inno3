using DocumentFormat.OpenXml.Packaging;
using System.Text;

namespace MoodleIndexer.Services;

public class PowerPointExtractor
{
    public async Task<string> ExtractFromUrl(string url)
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            return ExtractTextFromStream(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] PPTX aus URL fehlgeschlagen: {ex.Message}");
            return "";
        }
    }

    public string ExtractFromLocalHash(string contentHash, string basePath)
    {
        var sub1 = contentHash[..2];
        var sub2 = contentHash.Substring(2, 2);
        var fullPath = Path.Combine(basePath, sub1, sub2, contentHash);

        Console.WriteLine($"[INFO] Lade lokale PPTX: {fullPath}");

        if (!File.Exists(fullPath))
        {
            Console.WriteLine($"[WARN] Datei nicht gefunden: {fullPath}");
            return "";
        }

        try
        {
            using var stream = File.OpenRead(fullPath);
            return ExtractTextFromStream(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] PPTX Parsing fehlgeschlagen ({contentHash}): {ex.Message}");
            return "";
        }
    }

    private string ExtractTextFromStream(Stream stream)
    {
        var sb = new StringBuilder();

        using var ppt = PresentationDocument.Open(stream, false);
        var slides = ppt.PresentationPart?.SlideParts;
        if (slides == null) return "";

        foreach (var slide in slides)
        {
            var texts = slide.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Select(t => t.Text);
            foreach (var t in texts)
            {
                sb.AppendLine(t);
            }
        }

        return sb.ToString().Trim();
    }
}
