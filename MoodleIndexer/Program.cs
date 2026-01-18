using Microsoft.Extensions.Configuration;
using MoodleIndexer.Services;

// WICHTIG: AddEnvironmentVariables() hinzufügen, damit Docker-Einstellungen greifen!
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true) // Optional, falls im Docker nicht vorhanden
    .AddEnvironmentVariables() // <--- DAS HAT GEFEHLT
    .Build();

var pdfExtractor = new PdfExtractor();
var wordExtractor = new WordExtractor();
var pptExtractor = new PowerPointExtractor();
var textExtractor = new TextExtractor();
var csvExtractor = new CsvExtractor();
var excelExtractor = new ExcelExtractor();
var jsonExtractor = new JsonExtractor();
var xmlExtractor = new XmlExtractor();
var htmlExtractor = new HtmlExtractor();
var zipExtractor = new ZipExtractor();

var moodleService = new MoodleService(
    config,
    pdfExtractor,
    wordExtractor,
    pptExtractor,
    textExtractor,
    csvExtractor,
    excelExtractor,
    jsonExtractor,
    xmlExtractor,
    htmlExtractor,
    zipExtractor
);

var indexService = new IndexService(config);

// Wartezeit für den ersten Docker-Start (bis zu 10 Minuten)
const int maxRetries = 60;
const int delaySeconds = 10;

int attempt = 0;
bool success = false;

Console.WriteLine("🚀 Indexer gestartet. Warte auf Moodle Datenbank...");

while (attempt < maxRetries && !success)
{
    try
    {
        attempt++;
        // Wir prüfen erst connection
        var courses = await moodleService.FetchCourses();

        if (courses.Count > 0) {
            Console.WriteLine($"✅ Erfolg! {courses.Count} Kurse extrahiert. Sende an Meilisearch...");
            await indexService.IndexCoursesAsync(courses);
            success = true;
        } else {
            Console.WriteLine($"⚠ Verbindung steht, aber noch 0 Kurse gefunden (Versuch {attempt}/{maxRetries}). Moodle ist wohl noch leer oder installiert...");

            // Wenn Verbindung klappt, aber 0 Kurse da sind, brechen wir nicht ab, sondern warten kurz
            // Vielleicht sind wir schneller fertig, wenn Moodle leer ist.
            if (attempt > 5) {
                 Console.WriteLine("ℹ️ Moodle scheint leer zu sein (0 Kurse). Beende Indexierung erfolgreich.");
                 success = true; // Wir tun so als ob alles ok ist, damit der Container nicht crasht
            } else {
                 await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⏳ Moodle noch nicht bereit (Versuch {attempt}/{maxRetries}): {ex.Message}");

        if (attempt < maxRetries)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
        else
        {
            Console.WriteLine("❌ Maximalanzahl an Versuchen erreicht. Moodle braucht zu lange oder ist kaputt.");
        }
    }
}

if (success) {
    Console.WriteLine("🎉 Indexierung erfolgreich abgeschlossen.");
} else {
    Console.WriteLine("💀 Indexierung fehlgeschlagen.");
}