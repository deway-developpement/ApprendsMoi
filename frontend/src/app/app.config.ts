import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withInMemoryScrolling } from '@angular/router';

import { routes } from './app.routes';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';

export const appConfig: ApplicationConfig = {
  providers: [provideZoneChangeDetection({ eventCoalescing: true }), 
    provideRouter(routes, 
      withInMemoryScrolling({
        anchorScrolling: 'enabled', // Active le scroll vers les IDs
        scrollPositionRestoration: 'enabled' // Remonte en haut lors d'un changement de page
      })), 
    provideClientHydration(withEventReplay())]
};
