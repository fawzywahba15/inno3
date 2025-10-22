import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Course } from '../models/course.model';

@Injectable({ providedIn: 'root' })
export class SearchService {
  private readonly API_URL = 'http://localhost:7700/indexes/courses/search';
  private readonly headers = new HttpHeaders({
    'Content-Type': 'application/json',
    Authorization: 'Bearer key123',
  });

  constructor(private http: HttpClient) {}

  search(query: string, sortBy: string): Observable<{ hits: Course[] }> {
    return this.http.post<{ hits: Course[] }>(
      this.API_URL,
      { q: query, sort: [`${sortBy}:asc`] },
      { headers: this.headers }
    );
  }
}
