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

    // Extractors
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

        // Priority: Docker Env Variable > AppSettings
        _moodleDataDir = config["Moodle:FileDataPath"]!;

        var envConnString = config["MoodleDbConnectionString"];
        if (!string.IsNullOrEmpty(envConnString))
        {
            _connectionString = envConnString;
            Console.WriteLine("🔧 Nutze ConnectionString aus Umgebungsvariablen.");
        }
        else
        {
            var db = config.GetSection("Database");
            _connectionString = $"Server={db["Host"]};Port={db["Port"]};Database={db["Database"]};Uid={db["User"]};Pwd={db["Password"]};";
        }
    }

    public async Task<List<Course>> FetchCourses()
    {
        var courses = new List<Course>();

        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("SELECT id, fullname, shortname, summary FROM mdl_course WHERE id > 1", conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            courses.Add(new Course
            {
                Id = reader.GetInt32("id"),
                Title = reader.GetString("fullname"),
                Shortname = reader.GetString("shortname"),
                Description = reader["summary"]?.ToString() ?? ""
            });
        }
        await reader.CloseAsync();

        foreach (var course in courses)
        {
            Console.WriteLine($"🔍 Verarbeite Kurs {course.Id}: {course.Title}");
            course.Sections = FetchSections(course.Id);
            course.Pages = FetchPages(course.Id);
            course.Books = FetchBooks(course.Id);
            course.Files = await FetchFiles(course.Id);
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

        // Hier lassen wir 'visible=1' vorerst drin, damit wir nicht leere Sektionen indexieren, Fokus liegt auf Dateien.
        using var cmd = new MySqlCommand("SELECT section, name, summary FROM mdl_course_sections WHERE course = @course AND visible = 1", conn);
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

        using var cmd = new MySqlCommand(@"
            SELECT p.name, p.content
            FROM mdl_page p
            JOIN mdl_course_modules cm ON cm.instance = p.id
            JOIN mdl_modules m ON m.id = cm.module
            WHERE cm.course = @course AND m.name = 'page' AND cm.visible = 1", conn);
        cmd.Parameters.AddWithValue("@course", courseId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            pages.Add(new Page { Name = reader["name"]?.ToString(), Content = reader["content"]?.ToString() });
        }
        return pages;
    }

    private List<Book> FetchBooks(int courseId)
    {
        var books = new List<Book>();
        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = new MySqlCommand(@"
            SELECT b.name AS book_name, ch.title AS chapter_title, ch.content
            FROM mdl_book_chapters ch
            JOIN mdl_book b ON b.id = ch.bookid
            JOIN mdl_course_modules cm ON cm.instance = b.id
            JOIN mdl_modules m ON m.id = cm.module
            WHERE cm.course = @course AND m.name = 'book' AND cm.visible = 1", conn);
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

    private async Task<List<CourseFile>> FetchFiles(int courseId)
    {
        var files = new List<CourseFile>();

        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        // Wir laden ALLE Dateien, auch versteckte (visible=0)
        // Wir holen das Feld 'cm.visible' als 'is_visible'
        using var cmd = new MySqlCommand("""
            SELECT f.filename, f.filesize, f.mimetype, f.filepath, f.timemodified,
                   f.contextid, f.filearea, f.component, f.contenthash,
                   cm.instance AS itemid,
                   cm.visible AS is_visible
            FROM mdl_files f
            JOIN mdl_context ctx ON ctx.id = f.contextid
            JOIN mdl_course_modules cm ON ctx.instanceid = cm.id
            JOIN mdl_modules m ON m.id = cm.module
            WHERE cm.course = @course
              AND f.filename != '.'
              AND f.filesize > 0
              AND f.component LIKE 'mod_%'
        """, conn);
        cmd.Parameters.AddWithValue("@course", courseId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var filename = reader["filename"].ToString()!;
            var filepath = reader["filepath"].ToString()!;
            var mimetype = reader["mimetype"].ToString()!;
            var contentHash = reader["contenthash"].ToString()!;

            // SPRINT 3: Sichtbarkeit auslesen (1 = sichtbar, 0 = versteckt)
            bool isVisible = reader.GetInt32("is_visible") == 1;

            var url = BuildFileUrl(reader);
            string text = "";
            var tempFiles = new List<CourseFile>();

            // --- ZIP SPECIAL HANDLING ---
            if (mimetype.Contains("zip"))
            {
                Console.WriteLine($"📦 ZIP wird verarbeitet: {filename} (Sichtbar: {isVisible})");
                try
                {
                    // Versuche erst lokal zu laden (Performanter)
                    var p1 = contentHash.Substring(0, 2);
                    var p2 = contentHash.Substring(2, 2);
                    var localPath = Path.Combine(_moodleDataDir, p1, p2, contentHash);

                    List<ZipExtractor.ExtractedFile> extracted = new();

                    if (File.Exists(localPath))
                    {
                         // Wenn lokal vorhanden, lese Bytes und entpacke im Speicher
                         Console.WriteLine($"[INFO] Lade ZIP lokal: {localPath}");
                         var zipBytes = await File.ReadAllBytesAsync(localPath);
                         // Hier bräuchten wir eigentlich eine Methode ExtractFromBytes im ZipExtractor
                         // Fallback auf URL, da ZipExtractor aktuell nur URL kann:
                         extracted = await _zipExtractor.ExtractFromUrlAsync(url);
                    }
                    else
                    {
                         extracted = await _zipExtractor.ExtractFromUrlAsync(url);
                    }

                    foreach (var entry in extracted)
                    {
                        var extractedText = ProcessExtractedFile(entry.Filename, entry.ContentBytes);
                        if (!string.IsNullOrWhiteSpace(extractedText))
                        {
                            tempFiles.Add(new CourseFile
                            {
                                Filename = $"{filename}/{entry.Filename}",
                                Filepath = filepath,
                                Filesize = entry.ContentBytes.Length,
                                Mimetype = GetMimeTypeFromExtension(entry.Filename),
                                Module = reader["component"].ToString()!.Replace("mod_", ""),
                                Filearea = reader["filearea"].ToString()!,
                                Timemodified = reader.GetInt64("timemodified"),
                                Url = url,
                                Text = extractedText,
                                Visible = isVisible // Kind erbt Sichtbarkeit
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Fehler bei ZIP {filename}: {ex.Message}");
                    _errorCount++;
                }
            }
            else
            {
                // --- STANDARD HANDLING (PDF, WORD, TEXT ...) ---
                // Wir nutzen die Hilfsmethode, die automatisch lokal vs URL prüft
                text = await TryExtractText(mimetype, url, contentHash, filename);

                // Füge Datei zur Liste hinzu
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
                    Text = text,
                    Visible = isVisible // SPRINT 3: Property setzen
                });
            }

            if (tempFiles.Any()) files.AddRange(tempFiles);
        }

        return files;
    }

    /// <summary>
    /// Versucht Text zu extrahieren. Priorisiert lokale Datei (via Volume Mount),
    /// falls das fehlschlägt, versucht es den URL Download.
    /// </summary>
    private async Task<string> TryExtractText(string mimetype, string url, string contentHash, string filename)
    {
        // 1. Versuch: Lokale Datei (Performance & Stabilität)
        try
        {
            var p1 = contentHash.Substring(0, 2);
            var p2 = contentHash.Substring(2, 2);
            var localPath = Path.Combine(_moodleDataDir, p1, p2, contentHash);

            if (File.Exists(localPath))
            {
                Console.WriteLine($"[INFO] Lokale Datei gefunden: {filename}");
                var bytes = await File.ReadAllBytesAsync(localPath);

                if (mimetype == "application/pdf")
                    return _pdfExtractor.ExtractFromBytes(bytes);
                if (mimetype.Contains("word") || mimetype.Contains("document"))
                    return _wordExtractor.ExtractFromBytes(bytes);
                if (mimetype.Contains("presentation") || mimetype.Contains("powerpoint"))
                    return _pptExtractor.ExtractFromBytes(bytes);
                if (mimetype.Contains("excel") || mimetype.Contains("spreadsheet"))
                    return _excelExtractor.ExtractFromBytes(bytes);
                if (mimetype.StartsWith("text/") || mimetype.Contains("json") || mimetype.Contains("xml") || mimetype.Contains("html"))
                {
                     // Einfache Text-Formate
                     if (mimetype.Contains("html")) return _htmlExtractor.ExtractFromBytes(bytes);
                     if (mimetype.Contains("json")) return _jsonExtractor.ExtractFromBytes(bytes);
                     if (mimetype.Contains("xml")) return _xmlExtractor.ExtractFromBytes(bytes);
                     return _textExtractor.ExtractFromBytes(bytes);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Lokaler Zugriff fehlgeschlagen für {filename}: {ex.Message}. Versuche URL...");
        }

        // 2. Versuch: URL Download (Fallback)
        try
        {
            if (mimetype == "application/pdf") return await _pdfExtractor.ExtractFromUrl(url);
            if (mimetype.Contains("word")) return await _wordExtractor.ExtractFromUrl(url);
            if (mimetype.Contains("presentation")) return await _pptExtractor.ExtractFromUrl(url);
            if (mimetype.Contains("excel")) return await _excelExtractor.ExtractFromUrl(url);
            if (mimetype.StartsWith("text/") || mimetype.Contains("json") || mimetype.Contains("xml")) return await _textExtractor.ExtractFromUrl(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] URL Download auch fehlgeschlagen für {filename}: {ex.Message}");
            _errorCount++;
        }

        return "";
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

        var url = string.Join("/", parts);

        // --- DOCKER FIXES ---
        // Webservice Pfad korrigieren
        url = url.Replace("/pluginfile.php/", "/webservice/pluginfile.php/");

        // Hostname korrigieren: Browser nutzt localhost, Container nutzt 'moodle'
        url = url.Replace("localhost:8081", "moodle:8080");

        return $"{url}?token={_token}";
    }

    private string ProcessExtractedFile(string filename, byte[] fileBytes)
    {
        var extension = Path.GetExtension(filename).ToLowerInvariant();
        string text = "";
        try
        {
            switch (extension)
            {
                case ".pdf": text = _pdfExtractor.ExtractFromBytes(fileBytes); break;
                case ".docx": text = _wordExtractor.ExtractFromBytes(fileBytes); break;
                case ".pptx": text = _pptExtractor.ExtractFromBytes(fileBytes); break;
                case ".txt": text = _textExtractor.ExtractFromBytes(fileBytes); break;
                case ".csv": text = _csvExtractor.ExtractFromBytes(fileBytes); break;
                case ".xlsx": case ".xls": text = _excelExtractor.ExtractFromBytes(fileBytes); break;
                case ".json": text = _jsonExtractor.ExtractFromBytes(fileBytes); break;
                case ".xml": text = _xmlExtractor.ExtractFromBytes(fileBytes); break;
                case ".html": case ".htm": text = _htmlExtractor.ExtractFromBytes(fileBytes); break;
                default: Console.WriteLine($"[WARN] Dateiendung {extension} in ZIP nicht unterstützt."); return "";
            }
        }
        catch (Exception ex) { Console.WriteLine($"[ERROR] Fehler beim Extrahieren aus ZIP: {ex.Message}"); }
        return text;
    }

    private string GetMimeTypeFromExtension(string filename)
    {
        var extension = Path.GetExtension(filename).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".html" => "text/html",
            ".htm" => "text/html",
            _ => "application/octet-stream"
        };
    }
}