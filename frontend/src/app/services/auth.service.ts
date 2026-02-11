import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of, tap, switchMap, catchError } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../environments/environment';

// --- MODELS ---

export enum ProfileType {
  Admin = 0,
  Teacher = 1,
  Parent = 2,
  Student = 3
}

export enum GradeLevel {
  CP = 0,
  CE1 = 1,
  CE2 = 2,
  CM1 = 3,
  CM2 = 4,
  Sixieme = 5,
  Cinquieme = 6,
  Quatrieme = 7,
  Troisieme = 8,
  Seconde = 9,
  Premiere = 10,
  Terminale = 11
}

export interface LoginRequest {
  credential?: string; // Email ou Username
  password?: string;
}

export interface RegisterRequest {
  email?: string;
  password?: string;
  firstName?: string;
  lastName?: string;
  profile: ProfileType;
  phoneNumber?: string;
}

export interface RegisterStudentRequest {
  username?: string;
  password?: string;
  firstName?: string;
  lastName?: string;
  gradeLevel: GradeLevel;
  birthDate?: string; // Format YYYY-MM-DD
  parentId?: string; // UUID
}

export interface RefreshTokenRequest {
  refreshToken?: string;
}

export interface UserDto {
  id: string; // Changement: number -> string (UUID selon Swagger)
  email?: string;
  username?: string;
  firstName?: string;
  lastName?: string;
  profilePicture?: string;
  profileType: ProfileType;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
  // Teacher-specific fields
  verificationStatus?: 0 | 1 | 2 | 3; // 0=PENDING, 1=VERIFIED, 2=REJECTED, 3=DIPLOMA_VERIFIED
  bio?: string;
  phoneNumber?: string;
  isPremium?: boolean;
  city?: string;
  travelRadiusKm?: number;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  user: UserDto;
}

// --- SERVICE ---

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly API_URL = environment.apiUrl;

  private currentUserSubject = new BehaviorSubject<UserDto | null>(null);
  currentUser$ = this.currentUserSubject.asObservable();

  constructor() {}

  initializeUser(): Observable<any> {
    if (!isPlatformBrowser(this.platformId)) {
      return of(null);
    }

    const token = this.getToken();

    if (!token) {
      return of(null);
    }

    // On utilise fetchMe pour vérifier si le token est toujours valide et récupérer les infos à jour
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

  registerStudent(data: RegisterStudentRequest): Observable<any> {
    return this.http.post(`${this.API_URL}/api/Auth/register/student`, data);
  }

  login(credentials: LoginRequest): Observable<UserDto> {
    return this.http.post<LoginResponse>(`${this.API_URL}/api/Auth/login`, credentials).pipe(
      tap(res => {
        this.storeTokens(res.token, res.refreshToken);
        // Le Swagger indique que la LoginResponse contient déjà l'objet UserDto, 
        // on peut donc mettre à jour le sujet directement sans refaire un appel fetchMe()
        if (res.user) {
          this.currentUserSubject.next(res.user);
        }
      }),
      // Si l'objet user n'est pas complet dans la réponse login, on peut garder switchMap
      // switchMap(() => this.fetchMe()), 
      // Mais ici on renvoie l'utilisateur directement s'il est présent
      switchMap((res) => res.user ? of(res.user) : this.fetchMe()),
      tap(user => this.redirectUser(user.profileType))
    );
  }

  hasPasswordComplexity(password: string): boolean {
    if (password.length < 6) return false;

    const hasUpper = /[A-Z]/.test(password);
    const hasLower = /[a-z]/.test(password);
    const hasDigit = /\d/.test(password);

    return hasUpper && hasLower && hasDigit;
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
        this.currentUserSubject.next(user);
      }),
      catchError(err => {
        if (isPlatformBrowser(this.platformId)) {
           this.logout();
        }
        throw err;
      })
    );
  }

  private redirectUser(profile: ProfileType): void {
    // Redirection simple pour l'instant
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

  // --- Gestion des Tokens ---

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