import { Routes } from '@angular/router';
import { Visio } from './pages/Visio/Visio.component';
import { PlanningComponent } from './pages/Visio/Planning/planning.component';
import { ConnexionComponent } from './pages/Connexion/connexion.component';
import { InscriptionComponent } from './pages/Inscription/inscription.component';
import { HomeRouterComponent } from './pages/HomeRooter/home-router.component';
import { ChatComponent } from './pages/Chat/chat.component';
import { UsersListComponent } from './pages/HomeAdmin/UsersList/users-list.component';
import { UserDetailsComponent } from './pages/HomeAdmin/UsersDetails/user-details.component';

export const routes: Routes = [
  { path: '', component: HomeRouterComponent },
  { path: 'visio', component: PlanningComponent },
  { 
    path: 'visio/:id', 
    component: Visio,
    data: { getPrerenderParams: () => [] }
  },
  { path: 'login', component: ConnexionComponent},
  { path: 'register', component: InscriptionComponent },
  { path: 'chat', component: ChatComponent },
  { path: 'admin/users', component: UsersListComponent },
  { path: 'admin/users/:id', component: UserDetailsComponent },
];


