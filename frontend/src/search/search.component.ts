// search.component.ts
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';

import {
  MatFormFieldModule,
  // @ts-ignore
  MatInputModule,
  // @ts-ignore
  MatSelectModule
} from '@angular/material/form-field';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatOption } from '@angular/material/core';

import { SearchService } from '../services/search.service';
import { Course } from '../models/course.model';
import { stripHtml, getReadableSize } from '../utils/utils';
import {MatInput} from '@angular/material/input';
import {MatSelect} from '@angular/material/select';
import { debounceTime, Subject } from 'rxjs';


@Component({
  selector: 'app-search',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    HttpClientModule,
    MatFormFieldModule,
    MatButtonModule,
    MatProgressBarModule,
    MatCardModule,
    MatIconModule,

    MatInput

  ],
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.css']
})
export class SearchComponent {
  query = '';
  results: Course[] = [];
  loading = false;
  sortBy = 'title';
  filterCategory = '';
  allExpanded = false;
  expanded: Record<string, boolean> = {};



  private searchSubject = new Subject<string>();


  ngOnInit() {
    this.searchSubject.pipe(debounceTime(300)).subscribe(q => {
      this.query = q;
      this.searchCourses();
    });
  }
  onQueryChange(q: string) {
    this.searchSubject.next(q);
  }



  constructor(private searchService: SearchService) {}

  stripHtml = stripHtml;
  getReadableSize = getReadableSize;

  searchCourses(): void {
    if (!this.query.trim()) return;
    this.loading = true;

    this.searchService.search(this.query, this.sortBy).subscribe({
      next: (res) => {
        const filtered = this.filterCategory
          ? res.hits.filter(course => (course as any).category === this.filterCategory)
          : res.hits;

        this.results = filtered;

        // Init expanded-state für jeden Kurs-Abschnitt
        for (const course of this.results) {
          const keys = ['sections', 'pages', 'books', 'files'];
          for (const key of keys) {
            // @ts-ignore
            const match = this.hasMatching(key, course);
            this.expanded[`${key}-${course.id}`] = match;
          }
        }

        this.loading = false;
      },
      error: (err) => {
        console.error('Fehler bei der Suche:', err);
        this.loading = false;
      }
    });
  }

  highlight(text: string | null | undefined): string {
    if (!text) return '';
    if (!this.query) return text;

    const escaped = this.query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const regex = new RegExp(`\\b(${escaped})\\b`, 'gi');

    return text.replace(regex, '<mark>$1</mark>');
  }


  matchesQuery(text: string): boolean {
    return text?.toLowerCase().includes(this.query.toLowerCase());
  }

  toggle(key: string): void {
    this.expanded[key] = !this.expanded[key];
  }

  isExpanded(key: string): boolean {
    return this.expanded[key] ?? false;
  }

  getToggleIcon(key: string): string {
    return this.isExpanded(key) ? 'expand_less' : 'expand_more';
  }

  hasMatching(type: 'sections' | 'pages' | 'books' | 'files', course: Course): boolean {
    const items = course[type];
    if (!items?.length) return false;

    return items.some(item => {
      if (type === 'sections') {
        //@ts-ignore
        return this.matchesQuery(item.name) || this.matchesQuery(stripHtml(item.summary));
      }
      if (type === 'pages') {
        //@ts-ignore
        return this.matchesQuery(item.name) || this.matchesQuery(stripHtml(item.content));
      }
      if (type === 'books') {
        return (
          //@ts-ignore
          this.matchesQuery(item.book_name) ||
          //@ts-ignore
          this.matchesQuery(item.chapter_title) ||
          //@ts-ignore
          this.matchesQuery(stripHtml(item.content))
        );
      }
      if (type === 'files') {
        //@ts-ignore
        return this.matchesQuery(item.filename) || this.matchesQuery(item.text);
      }
      return false;
    });
  }


  hasVisibleSections(course: Course): boolean {
    return course.sections?.some(s =>
      (s.name && s.name.trim()) ||
      (s.summary && stripHtml(s.summary).trim())
    );
  }


  cleanAndHighlight(html: string | null | undefined): string {
    if (!html) return '';
    const plain = stripHtml(html);
    return this.highlight(plain);
  }


  getHighlightedSnippets(text: string, query: string, contextChars = 80): string[] {
    if (!text || !query) return [];
    const plain = stripHtml(text);
    const regex = new RegExp(`(.{0,${contextChars}})(${query})(.{0,${contextChars}})`, 'gi');
    const matches = [...plain.matchAll(regex)];
    return matches.slice(0, 3).map(m =>
      `...${m[1]}<mark>${m[2]}</mark>${m[3]}...`.replace(/\n/g, '<br>')
    );
  }


  highlightWithBreaks(text: string | null | undefined): string {
    if (!text) return '';
    const highlighted = this.highlight(text);
    return highlighted.replace(/\n/g, '<br>');
  }




  toggleAll() {
    this.allExpanded = !this.allExpanded;
    const keys = Object.keys(this.expanded);
    for (const key of keys) {
      this.expanded[key] = this.allExpanded;
    }
  }

}
