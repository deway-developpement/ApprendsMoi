import { Routes } from '@angular/router';
import { HomeComponent } from './pages/Home/Home.component';
import { ConnexionComponent } from './pages/Connexion/connexion.component';
import { InscriptionComponent } from './pages/Inscription/inscription.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'login', component: ConnexionComponent},
  { path: 'register', component: InscriptionComponent }
];