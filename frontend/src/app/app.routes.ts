import { Routes } from '@angular/router';
import { Home } from './pages/Home/Home.component';
import { Visio } from './pages/Visio/Visio.component';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'visio', component: Visio }
];
