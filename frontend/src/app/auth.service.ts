import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';

interface LoginResponse {
  token: string;
  username: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private tokenKey = 'auth_token';

  login(username: string, password: string) {
    return this.http.post<LoginResponse>('/api/auth/login', { username, password }).pipe(
      tap(res => localStorage.setItem(this.tokenKey, res.token))
    );
  }

  logout() {
    localStorage.removeItem(this.tokenKey);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }
}
