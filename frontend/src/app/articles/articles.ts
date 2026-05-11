import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { Article, ArticleService, BookingType } from '../article.service';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-articles',
  imports: [CommonModule, FormsModule],
  templateUrl: './articles.html',
  styleUrl: './articles.scss'
})
export class Articles implements OnInit {
  private svc = inject(ArticleService);
  private auth = inject(AuthService);
  private router = inject(Router);

  articles = signal<Article[]>([]);
  loading = signal(false);
  message = signal<{ text: string; error: boolean } | null>(null);

  showModal = signal(false);
  selectedArticle = signal<Article | null>(null);
  bookingType = signal<BookingType>('Einbuchen');
  amount = signal(1);

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.svc.getAll().subscribe({
      next: data => { this.articles.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  openModal(article: Article) {
    this.selectedArticle.set(article);
    this.bookingType.set('Einbuchen');
    this.amount.set(1);
    this.message.set(null);
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
    this.selectedArticle.set(null);
  }

  confirm() {
    const art = this.selectedArticle();
    if (!art) return;

    this.svc.book(art.id, { type: this.bookingType(), amount: this.amount() }).subscribe({
      next: () => {
        this.closeModal();
        this.load();
        this.message.set({ text: 'Buchung erfolgreich.', error: false });
      },
      error: (err: HttpErrorResponse) => {
        this.message.set({ text: err.error ?? 'Fehler bei der Buchung.', error: true });
      }
    });
  }
}
