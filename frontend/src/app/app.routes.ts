import { Routes } from '@angular/router';
import { Visio } from './pages/Visio/Visio.component';
import { PlanningComponent } from './pages/Visio/Planning/planning.component';
import { ConnexionComponent } from './pages/Connexion/connexion.component';
import { InscriptionComponent } from './pages/Inscription/inscription.component';
import { HomeRouterComponent } from './pages/HomeRooter/home-router.component';

export const routes: Routes = [
  { path: '', component: HomeRouterComponent },
  { path: 'visio', component: PlanningComponent },
  { path: 'visio/:id', component: Visio },
  { path: 'login', component: ConnexionComponent},
  { path: 'register', component: InscriptionComponent }
];
