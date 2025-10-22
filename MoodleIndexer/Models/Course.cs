namespace MoodleIndexer.Models;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Shortname { get; set; } = "";
    public string Description { get; set; } = "";
    public List<Section> Sections { get; set; } = new();
    public List<Page> Pages { get; set; } = new();
    public List<Book> Books { get; set; } = new();
    public List<CourseFile> Files { get; set; } = new();
}

public class Section
{
    public int SectionNumber { get; set; }
    public string? Name { get; set; }
    public string? Summary { get; set; }
}

public class Page
{
    public string? Name { get; set; }
    public string? Content { get; set; }
}

public class Book
{
    public string? BookName { get; set; }
    public string? ChapterTitle { get; set; }
    public string? Content { get; set; }
}

public class CourseFile
{
    public string Filename { get; set; } = "";
    public string Filepath { get; set; } = "";
    public long Filesize { get; set; }
    public string Mimetype { get; set; } = "";
    public string Module { get; set; } = "";
    public string Filearea { get; set; } = "";
    public long Timemodified { get; set; }
    public string Url { get; set; } = "";
    public string Text { get; set; } = "";
}
