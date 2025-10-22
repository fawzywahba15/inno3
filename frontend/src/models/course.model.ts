export interface CourseFile {
  filename: string;
  filepath: string;
  filesize: number;
  mimetype: string;
  module: string;
  filearea: string;
  timemodified: number;
  url: string;
  text: string;
}

export interface CourseSection {
  section: number;
  name: string;
  summary: string;
}

export interface CoursePage {
  name: string;
  content: string;
}

export interface CourseBook {
  book_name: string;
  chapter_title: string;
  content: string;
}

export interface Course {
  id: number;
  title: string;
  shortname: string;
  description: string;
  sections: CourseSection[];
  pages: CoursePage[];
  books: CourseBook[];
  files: CourseFile[];
}
