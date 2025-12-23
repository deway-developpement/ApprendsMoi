import { Routes } from '@angular/router';
import { Home } from './pages/Home/Home.component';
import { Visio } from './pages/Visio/Visio.component';
import { PlanningComponent } from './pages/Visio/Planning/planning.component';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'visio', component: PlanningComponent },
  { path: 'visio/:id', component: Visio }
];
