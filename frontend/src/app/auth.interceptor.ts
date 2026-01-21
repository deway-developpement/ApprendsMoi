import { HttpErrorResponse, HttpInterceptorFn, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService, LoginResponse } from './services/auth.service'; // Ajustez le chemin
import { catchError, switchMap, throwError, BehaviorSubject, filter, take } from 'rxjs';

// Variables d'état pour la gestion de la file d'attente (Mutex)
let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  // 1. Ajouter le token s'il existe
  let authReq = req;
  if (token) {
    authReq = addTokenHeader(req, token);
  }

  // 2. Gérer la réponse
  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // Si erreur 401 (Non autorisé) et qu'on n'essaie pas déjà de se connecter ou refresh
      if (error.status === 401 && !authReq.url.includes('/Auth/login') && !authReq.url.includes('/Auth/refresh')) {
        return handle401Error(authReq, next, authService);
      }
      return throwError(() => error);
    })
  );
};

// Helper pour ajouter le header
const addTokenHeader = (request: HttpRequest<any>, token: string) => {
  return request.clone({
    setHeaders: { Authorization: `Bearer ${token}` } //
  });
};

// Logique complexe de rafraîchissement
const handle401Error = (request: HttpRequest<any>, next: HttpHandlerFn, authService: AuthService) => {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null); // On bloque les autres requêtes

    return authService.refreshToken().pipe(
      switchMap((tokenResponse: LoginResponse) => {
        isRefreshing = false;
        refreshTokenSubject.next(tokenResponse.token); // On débloque la file
        return next(addTokenHeader(request, tokenResponse.token));
      }),
      catchError((err) => {
        isRefreshing = false;
        authService.logout(); // Si le refresh rate, on déconnecte
        return throwError(() => err);
      })
    );
  } else {
    // Si un refresh est déjà en cours, on attend qu'il finisse
    return refreshTokenSubject.pipe(
      filter(token => token !== null), // On attend que le token soit non-null
      take(1), // On prend la première valeur valide et on se désabonne
      switchMap((token) => {
        return next(addTokenHeader(request, token!));
      })
    );
  }
};