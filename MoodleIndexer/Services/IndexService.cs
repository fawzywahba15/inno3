using Meilisearch;
using Microsoft.Extensions.Configuration;
using MoodleIndexer.Models;

namespace MoodleIndexer.Services;

public class IndexService
{
    private readonly MeilisearchClient _client;
    private readonly string _indexName = "courses";

    public IndexService(IConfiguration config)
    {
        // Liest die Umgebungsvariable 'MeiliSearchUrl' aus Docker-Compose
        var url = config["MeiliSearchUrl"] ?? "http://localhost:7700";
        var key = config["MeiliSearchKey"] ?? "masterKeyMustBeLongEnough123";

        Console.WriteLine($"🔧 IndexService Config: URL={url}");

        _client = new MeilisearchClient(url, key);
    }

    public async Task IndexCoursesAsync(List<Course> courses)
    {
        try
        {
            var index = _client.Index(_indexName);

            // Index erstellen
            await _client.CreateIndexAsync(_indexName, "id");

            // Filtereinstellungen für Sprint 3 (Sichtbarkeit)
            await index.UpdateFilterableAttributesAsync(new[] { "files.visible", "category" });

            // Sortiereinstellungen
            await index.UpdateSortableAttributesAsync(new[] { "title", "id" });

            // Dokumente hochladen
            var task = await index.AddDocumentsAsync(courses);

            Console.WriteLine($"✅ {courses.Count} Kurse an MeiliSearch gesendet. Task UID: {task.TaskUid}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fehler beim Senden an Meilisearch: {ex.Message}");
        }
    }
}