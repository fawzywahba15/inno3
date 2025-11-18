using System.IO.Compression;
using System.Net.Http;

namespace MoodleIndexer.Services;

public class ZipExtractor
{
    private readonly HttpClient _httpClient = new();

    /// <summary>
    /// DTO, um extrahierte Datei-Bytes und Filename zurückzugeben
    /// </summary>
    public class ExtractedFile
    {
        public string Filename { get; set; } = "";
        public byte[] ContentBytes { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Lädt eine ZIP-Datei von einer URL herunter, entpackt sie im Speicher
    /// und gibt eine Liste der entpackten Dateien mit deren Inhalten zurück.
    /// </summary>
    public async Task<List<ExtractedFile>> ExtractFromUrlAsync(string url)
    {
        var extractedFiles = new List<ExtractedFile>();

        try
        {
            Console.WriteLine($"[INFO] Lade ZIP-Datei von URL: {url}");
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // Lade den Inhalt der ZIP-Datei als Stream
            await using var zipStream = await response.Content.ReadAsStreamAsync();

            // Erstelle ein ZipArchive aus dem Stream
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            foreach (var entry in archive.Entries)
            {
                // Ignoriere Ordner-Einträge und Dateien mit 0 Größe
                if (string.IsNullOrEmpty(entry.Name) || entry.Length == 0)
                {
                    continue;
                }
                
                // Lese den Inhalt der Datei in ein Byte-Array
                await using var entryStream = entry.Open();
                await using var memoryStream = new MemoryStream();
                await entryStream.CopyToAsync(memoryStream);
                
                extractedFiles.Add(new ExtractedFile
                {
                    Filename = entry.Name,
                    ContentBytes = memoryStream.ToArray()
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fehler beim Entpacken der ZIP-Datei von URL {url}: {ex.Message}");
        }

        return extractedFiles;
    }
}