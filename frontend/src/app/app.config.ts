import { ApplicationConfig, provideAppInitializer, inject, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { lastValueFrom } from 'rxjs';

// Imports locaux
import { routes } from './app.routes';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './services/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    
    provideRouter(routes, 
      withInMemoryScrolling({
        anchorScrolling: 'enabled',
        scrollPositionRestoration: 'enabled'
      })
    ),
    
    provideClientHydration(withEventReplay()),
    
    provideHttpClient(
      withInterceptors([authInterceptor])
    ),
    
    provideAnimations(),

    // ✅ NOUVELLE SYNTAXE (Remplace APP_INITIALIZER)
    provideAppInitializer(() => {
      const authService = inject(AuthService); // Injection directe
      return lastValueFrom(authService.initializeUser()); // On attend la fin de l'init
    })
  ]
};