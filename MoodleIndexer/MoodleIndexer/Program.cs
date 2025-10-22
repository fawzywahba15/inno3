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
await indexService.IndexCoursesAsync(courses);