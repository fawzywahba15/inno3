using Microsoft.Extensions.Configuration;
using MoodleIndexer.Models;
using MySqlConnector;

namespace MoodleIndexer.Services;

public class MoodleService
{
    private readonly IConfiguration _config;
    private readonly PdfExtractor _pdfExtractor;
    private readonly string _connectionString;
    private readonly string _baseUrl;
    private readonly string _token;
    private readonly string _moodleDataDir;

    private int _errorCount = 0;
    private readonly WordExtractor _wordExtractor;
    private readonly PowerPointExtractor _pptExtractor;
    private readonly TextExtractor _textExtractor;
    private readonly CsvExtractor _csvExtractor;
    private readonly ExcelExtractor _excelExtractor;
    private readonly JsonExtractor _jsonExtractor;
    private readonly XmlExtractor _xmlExtractor;
    private readonly HtmlExtractor _htmlExtractor;
    private readonly ZipExtractor _zipExtractor;


    public MoodleService(IConfiguration config, PdfExtractor pdf, WordExtractor word,
        PowerPointExtractor ppt, TextExtractor text, CsvExtractor csv, ExcelExtractor excel,
        JsonExtractor json, XmlExtractor xml, HtmlExtractor html, ZipExtractor zip)
    {
        _config = config;
        _pdfExtractor = pdf;
        _wordExtractor = word;
        _pptExtractor = ppt;
        _textExtractor = text;
        _csvExtractor = csv;
        _excelExtractor = excel;
        _jsonExtractor = json;
        _xmlExtractor = xml;
        _htmlExtractor = html;
        _zipExtractor = zip;
        
        _baseUrl = config["Moodle:BaseUrl"]!;
        _token = config["Moodle:Token"]!;
        _moodleDataDir = config["Moodle:FileDataPath"]!;
        var db = config.GetSection("Database");
        _connectionString =
            $"Server={db["Host"]};Port={db["Port"]};Database={db["Database"]};Uid={db["User"]};Pwd={db["Password"]};";
    }

    public List<Course> FetchCourses()
    {
        var courses = new List<Course>();

        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = new MySqlCommand("SELECT id, fullname, shortname, summary FROM mdl_course WHERE id > 1", conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            courses.Add(new Course
            {
                Id = reader.GetInt32("id"),
                Title = reader.GetString("fullname"),
                Shortname = reader.GetString("shortname"),
                Description = reader["summary"]?.ToString() ?? ""
            });
        }

        foreach (var course in courses)
        {
            Console.WriteLine($"🔍 Verarbeite Kurs {course.Id}: {course.Title}");
            course.Sections = FetchSections(course.Id);
            course.Pages = FetchPages(course.Id);
            course.Books = FetchBooks(course.Id);
            course.Files = FetchFiles(course.Id);
        }

        Console.WriteLine($" Insgesamt importierte Kurse: {courses.Count}");
        Console.WriteLine(_errorCount == 0
            ? " Keine Fehler aufgetreten."
            : $"️ Fehler beim Verarbeiten von {_errorCount} Datei(en).");

        return courses;
    }

    private List<Section> FetchSections(int courseId)
    {
        var sections = new List<Section>();

        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = new MySqlCommand("""
            SELECT section, name, summary
            FROM mdl_course_sections
            WHERE course = @course AND visible = 1
        """, conn);
        cmd.Parameters.AddWithValue("@course", courseId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            sections.Add(new Section
            {
                SectionNumber = reader.GetInt32("section"),
                Name = reader["name"]?.ToString(),
                Summary = reader["summary"]?.ToString()
            });
        }

        return sections;
    }

    private List<Page> FetchPages(int courseId)
    {
        var pages = new List<Page>();

        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = new MySqlCommand("""
            SELECT p.name, p.content
            FROM mdl_page p
            JOIN mdl_course_modules cm ON cm.instance = p.id
            JOIN mdl_modules m ON m.id = cm.module
            WHERE cm.course = @course AND m.name = 'page' AND cm.visible = 1
        """, conn);
        cmd.Parameters.AddWithValue("@course", courseId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            pages.Add(new Page
            {
                Name = reader["name"]?.ToString(),
                Content = reader["content"]?.ToString()
            });
        }

        return pages;
    }

    private List<Book> FetchBooks(int courseId)
    {
        var books = new List<Book>();

        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = new MySqlCommand("""
            SELECT b.name AS book_name, ch.title AS chapter_title, ch.content
            FROM mdl_book_chapters ch
            JOIN mdl_book b ON b.id = ch.bookid
            JOIN mdl_course_modules cm ON cm.instance = b.id
            JOIN mdl_modules m ON m.id = cm.module
            WHERE cm.course = @course AND m.name = 'book' AND cm.visible = 1
        """, conn);
        cmd.Parameters.AddWithValue("@course", courseId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            books.Add(new Book
            {
                BookName = reader["book_name"]?.ToString(),
                ChapterTitle = reader["chapter_title"]?.ToString(),
                Content = reader["content"]?.ToString()
            });
        }

        return books;
    }

    private List<CourseFile> FetchFiles(int courseId)
    {
        var files = new List<CourseFile>();

        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = new MySqlCommand("""
            SELECT f.filename, f.filesize, f.mimetype, f.filepath, f.timemodified,
                   f.contextid, f.filearea, f.component, f.contenthash,
                   cm.instance AS itemid
            FROM mdl_files f
            JOIN mdl_context ctx ON ctx.id = f.contextid
            JOIN mdl_course_modules cm ON ctx.instanceid = cm.id
            JOIN mdl_modules m ON m.id = cm.module
            WHERE cm.course = @course
              AND f.filename != '.'
              AND f.filesize > 0
              AND f.component LIKE 'mod_%'
              AND cm.visible = 1
        """, conn);
        cmd.Parameters.AddWithValue("@course", courseId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var filename = reader["filename"].ToString()!;
            var filepath = reader["filepath"].ToString()!;
            var mimetype = reader["mimetype"].ToString()!;
            var contentHash = reader["contenthash"].ToString()!;
            var url = BuildFileUrl(reader);

            string text = "";

            if (mimetype == "application/pdf")
            {
                Console.WriteLine($"📄 PDF wird extrahiert: {filename}");

                try
                {
                    text = _pdfExtractor.ExtractFromUrl(url).Result;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"🔁 Fallback auf lokale Datei (hash): {contentHash}");
                        text = _pdfExtractor.ExtractFromLocalHash(contentHash, _moodleDataDir);
                    }

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"⚠️ Keine extrahierbaren Inhalte in: {filename}");
                        _errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Fehler beim Extrahieren von {filename}: {ex.Message}");
                    _errorCount++;
                }
            }else if (mimetype == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            {
                Console.WriteLine($"[INFO] Word-Datei wird extrahiert: {filename}");
                try
                {
                    text = _wordExtractor.ExtractFromUrl(url).Result;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[INFO] Fallback auf lokale Word-Datei: {contentHash}");
                        text = _wordExtractor.ExtractFromLocalHash(contentHash, _moodleDataDir);
                    }

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[WARN] Keine extrahierbaren Inhalte in: {filename}");
                        _errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Fehler beim Word-Parsing: {ex.Message}");
                    _errorCount++;
                }
            }else if (mimetype == "application/vnd.openxmlformats-officedocument.presentationml.presentation")
            {
                Console.WriteLine($"[INFO] PPTX wird extrahiert: {filename}");
                try
                {
                    text = _pptExtractor.ExtractFromUrl(url).Result;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[INFO] Fallback auf lokale PPTX-Datei: {contentHash}");
                        text = _pptExtractor.ExtractFromLocalHash(contentHash, _moodleDataDir);
                    }

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[WARN] Keine extrahierbaren Inhalte in: {filename}");
                        _errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] PPTX Fehler: {ex.Message}");
                    _errorCount++;
                }
            }else if (mimetype == "text/plain")
            {
                Console.WriteLine($"[INFO] Text-Datei wird extrahiert: {filename}");
                try
                {
                    text = _textExtractor.ExtractFromUrl(url).Result;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[INFO] Fallback auf lokale Textdatei: {contentHash}");
                        text = _textExtractor.ExtractFromLocalHash(contentHash, _moodleDataDir);
                    }

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[WARN] Keine Inhalte in Textdatei: {filename}");
                        _errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Fehler bei Textdatei: {ex.Message}");
                    _errorCount++;
                }
            }else if (mimetype == "text/csv")
            {
                Console.WriteLine($"[INFO] CSV-Datei wird extrahiert: {filename}");
                try
                {
                    text = _csvExtractor.ExtractFromUrl(url).Result;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[INFO] Fallback auf lokale CSV-Datei: {contentHash}");
                        text = _csvExtractor.ExtractFromLocalHash(contentHash, _moodleDataDir);
                    }

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[WARN] Keine Inhalte in CSV-Datei: {filename}");
                        _errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Fehler bei CSV-Datei: {ex.Message}");
                    _errorCount++;
                }
            }else if (mimetype == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" ||
                      mimetype == "application/vnd.ms-excel")
            {
                Console.WriteLine($"[INFO] Excel-Datei wird extrahiert: {filename}");
                try
                {
                    text = _excelExtractor.ExtractFromUrl(url).Result;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[INFO] Fallback auf lokale Excel-Datei: {contentHash}");
                        text = _excelExtractor.ExtractFromLocalHash(contentHash, _moodleDataDir);
                    }

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[WARN] Keine Inhalte in Excel-Datei: {filename}");
                        _errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Fehler bei Excel-Datei: {ex.Message}");
                    _errorCount++;
                }
            }else if (mimetype == "application/json")
            {
                Console.WriteLine($"[INFO] JSON-Datei wird extrahiert: {filename}");
                try
                {
                    text = _jsonExtractor.ExtractFromUrl(url).Result;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[INFO] Fallback auf lokale JSON-Datei: {contentHash}");
                        text = _jsonExtractor.ExtractFromLocalHash(contentHash, _moodleDataDir);
                    }

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[WARN] Keine Inhalte in JSON-Datei: {filename}");
                        _errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Fehler bei JSON-Datei: {ex.Message}");
                    _errorCount++;
                }
            }else if (mimetype == "application/xml" || mimetype == "text/xml")
            {
                Console.WriteLine($"[INFO] XML-Datei wird extrahiert: {filename}");
                try
                {
                    text = _xmlExtractor.ExtractFromUrl(url).Result;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[INFO] Fallback auf lokale XML-Datei: {contentHash}");
                        text = _xmlExtractor.ExtractFromLocalHash(contentHash, _moodleDataDir);
                    }

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[WARN] Keine Inhalte in XML-Datei: {filename}");
                        _errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Fehler bei XML-Datei: {ex.Message}");
                    _errorCount++;
                }
            }else if (mimetype == "text/html")
            {
                Console.WriteLine($"[INFO] HTML-Datei wird extrahiert: {filename}");
                try
                {
                    text = _htmlExtractor.ExtractFromUrl(url).Result;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[INFO] Fallback auf lokale HTML-Datei: {contentHash}");
                        text = _htmlExtractor.ExtractFromLocalHash(contentHash, _moodleDataDir);
                    }

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[WARN] Keine Inhalte in HTML-Datei: {filename}");
                        _errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Fehler bei HTML-Datei: {ex.Message}");
                    _errorCount++;
                }
            }







            files.Add(new CourseFile
            {
                Filename = filename,
                Filepath = filepath,
                Filesize = reader.GetInt64("filesize"),
                Mimetype = mimetype,
                Module = reader["component"].ToString()!.Replace("mod_", ""),
                Filearea = reader["filearea"].ToString()!,
                Timemodified = reader.GetInt64("timemodified"),
                Url = url,
                Text = text
            });
        }

        return files;
    }

    private string BuildFileUrl(MySqlDataReader reader)
    {
        var filepath = reader["filepath"].ToString()!.Trim('/');
        var parts = new List<string>
        {
            _baseUrl,
            "pluginfile.php",
            reader["contextid"].ToString()!,
            reader["component"].ToString()!,
            reader["filearea"].ToString()!,
            reader["itemid"].ToString()!
        };

        if (!string.IsNullOrEmpty(filepath))
            parts.Add(filepath);

        parts.Add(reader["filename"].ToString()!);

        /*return $"{string.Join("/", parts)}?token={_token}";*/
        var url = string.Join("/", parts);
        url = url.Replace("/pluginfile.php/", "/webservice/pluginfile.php/");
        return $"{url}?token={_token}";

    }
}
