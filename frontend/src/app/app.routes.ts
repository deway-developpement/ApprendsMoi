import { Routes } from '@angular/router';
import { Home } from './pages/Home/Home.component';
import { Visio } from './pages/Visio/Visio.component';
import { PlanningComponent } from './pages/Visio/Planning/planning.component';
import { ConnexionComponent } from './pages/Connexion/connexion.component';
import { InscriptionComponent } from './pages/Inscription/inscription.component';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'visio', component: PlanningComponent },
  { path: 'visio/:id', component: Visio },
  { path: 'login', component: ConnexionComponent},
  { path: 'register', component: InscriptionComponent }
];
