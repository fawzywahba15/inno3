using Microsoft.Extensions.Configuration;
using MoodleIndexer.Services;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
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

// --- Wartezeit und Retry-Mechanismus, falls Moodle oder DB noch nicht bereit sind ---
const int maxRetries = 5;
const int delaySeconds = 10;
int attempt = 0;
bool success = false;

while (attempt < maxRetries && !success)
{
    try
    {
        attempt++;
        Console.WriteLine($"Versuch {attempt}/{maxRetries}: Kurse werden aus Moodle geladen...");

        var courses = moodleService.FetchCourses();
        Console.WriteLine($" {courses.Count} Kurse extrahiert. Sende an Meilisearch...");
        await indexService.IndexCoursesAsync(courses);

        success = true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Fehler beim Versuch {attempt}: {ex.Message}");
        if (attempt < maxRetries)
        {
            Console.WriteLine($" Warte {delaySeconds} Sekunden und versuche erneut...");
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
        else
        {
            Console.WriteLine(" Maximalanzahl an Versuchen erreicht. Beende Indexierung.");
        }
    }
}

Console.WriteLine(" Indexierung abgeschlossen oder abgebrochen.");

/*using Microsoft.Extensions.Configuration;
using MoodleIndexer.Services;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
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
    htmlExtractor // hier hinzufügen
);


var indexService = new IndexService(config);

Console.WriteLine(" Kurse werden aus Moodle geladen...");
var courses = moodleService.FetchCourses();

Console.WriteLine($" {courses.Count} Kurse extrahiert. Sende an Meilisearch...");
await indexService.IndexCoursesAsync(courses);*/