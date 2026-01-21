import { Injectable, inject, PLATFORM_ID } from '@angular/core'; // AJOUT: PLATFORM_ID
import { isPlatformBrowser } from '@angular/common'; // AJOUT: isPlatformBrowser
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of, tap, switchMap, catchError } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../environments/environment';

// --- MODELS (inchangés) ---
export enum ProfileType {
  Admin = 0,
  Teacher = 1,
  Parent = 2,
  Student = 3
}

export interface LoginRequest {
  credential?: string;
  password?: string;
  isStudent: boolean;
}

export interface RegisterRequest {
  email?: string;
  password?: string;
  profile: ProfileType;
}

export interface RefreshTokenRequest {
  refreshToken?: string;
}

export interface UserDto {
  id: number;
  email: string;
  username: string;
  profile: ProfileType;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
}

// --- SERVICE ---

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID); // AJOUT: Injection de l'ID de plateforme
  private readonly API_URL = environment.apiUrl;

  private currentUserSubject = new BehaviorSubject<UserDto | null>(null);
  currentUser$ = this.currentUserSubject.asObservable();

  constructor() {}

  initializeUser(): Observable<any> {
    // Si on est sur le serveur (SSR), on ne fait rien pour éviter le crash
    if (!isPlatformBrowser(this.platformId)) {
      return of(null);
    }

    const token = this.getToken();

    if (!token) {
      return of(null);
    }

    return this.fetchMe().pipe(
      catchError(() => {
        this.logout();
        return of(null);
      })
    );
  }

  register(data: RegisterRequest): Observable<any> {
    return this.http.post(`${this.API_URL}/api/Auth/register`, data);
  }

  login(credentials: LoginRequest): Observable<UserDto> {
    return this.http.post<LoginResponse>(`${this.API_URL}/api/Auth/login`, credentials).pipe(
      tap(res => this.storeTokens(res.token, res.refreshToken)),
      switchMap(() => this.fetchMe()),
      tap(user => this.redirectUser(user.profile))
    );
  }

  refreshToken(): Observable<LoginResponse> {
    const refreshToken = this.getRefreshToken();
    const payload: RefreshTokenRequest = { refreshToken: refreshToken || '' };

    return this.http.post<LoginResponse>(`${this.API_URL}/api/Auth/refresh`, payload).pipe(
      tap(res => this.storeTokens(res.token, res.refreshToken))
    );
  }

  fetchMe(): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.API_URL}/api/Auth/me`).pipe(
      tap(user => {
        console.log('User chargé:', user);
        this.currentUserSubject.next(user);
      }),
      catchError(err => {
        // On évite de logout si l'erreur vient du serveur lors du rendu initial
        if (isPlatformBrowser(this.platformId)) {
             this.logout();
        }
        throw err;
      })
    );
  }

  private redirectUser(profile: ProfileType): void {
    this.router.navigate(['/']);
  }

  logout(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem('token');
      localStorage.removeItem('refreshToken');
    }
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  // --- Gestion des Tokens (Sécurisée pour SSR) ---

  private storeTokens(token: string, refreshToken: string): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem('token', token);
      localStorage.setItem('refreshToken', refreshToken);
    }
  }

  getToken(): string | null {
    if (isPlatformBrowser(this.platformId)) {
      return localStorage.getItem('token');
    }
    return null;
  }

  getRefreshToken(): string | null {
    if (isPlatformBrowser(this.platformId)) {
      return localStorage.getItem('refreshToken');
    }
    return null;
  }
}