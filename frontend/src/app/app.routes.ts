import { Routes } from '@angular/router';
import { Home } from './pages/Home/Home.component';
import { ConnexionComponent } from './pages/Connexion/connexion.component';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'login', component: ConnexionComponent}
];