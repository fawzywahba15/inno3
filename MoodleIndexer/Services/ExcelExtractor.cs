using ClosedXML.Excel;

namespace MoodleIndexer.Services;

public class ExcelExtractor
{
    public async Task<string> ExtractFromUrl(string url)
    {
        try
        {
            Console.WriteLine($"[INFO] Lade Excel-Datei von URL: {url}");
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            return ExtractTextFromStream(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim Excel-Download: {ex.Message}");
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

            Console.WriteLine($"[INFO] Lade lokale Excel-Datei: {fullPath}");
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[WARN] Datei nicht gefunden: {fullPath}");
                return "";
            }

            using var fs = File.OpenRead(fullPath);
            return ExtractTextFromStream(fs);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim lokalen Excel-Parsing: {ex.Message}");
            return "";
        }
    }

    private string ExtractTextFromStream(Stream stream)
    {
        var result = new System.Text.StringBuilder();
        using var workbook = new XLWorkbook(stream);

        foreach (var sheet in workbook.Worksheets)
        {
            result.AppendLine($"### {sheet.Name} ###");
            foreach (var row in sheet.RowsUsed())
            {
                var cells = row.CellsUsed().Select(c => c.GetString().Trim());
                result.AppendLine(string.Join(" | ", cells));
            }
            result.AppendLine();
        }

        return result.ToString().Trim();
    }
    
    public string ExtractFromBytes(byte[] fileBytes)
    {
        try
        {
            using var ms = new MemoryStream(fileBytes);
            return ExtractTextFromStream(ms);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fehler beim In-Memory Excel-Parsing: {ex.Message}");
            return "";
        }
    }
}
