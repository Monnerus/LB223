import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Article {
  id: number;
  name: string;
  quantity: number;
}

export type BookingType = 'Einbuchen' | 'Ausbuchen';

export interface BookingRequest {
  type: BookingType;
  amount: number;
}

@Injectable({ providedIn: 'root' })
export class ArticleService {
  private http = inject(HttpClient);
  private base = '/api/articles';

  getAll(): Observable<Article[]> {
    return this.http.get<Article[]>(this.base);
  }

  book(id: number, request: BookingRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/book`, request);
  }
}
