import { Routes } from '@angular/router';
import { Visio } from './pages/Visio/Visio.component';
import { PlanningComponent } from './pages/Visio/Planning/planning.component';
import { ConnexionComponent } from './pages/Connexion/connexion.component';
import { InscriptionComponent } from './pages/Inscription/inscription.component';
import { HomeRouterComponent } from './pages/HomeRooter/home-router.component';
import { ChatComponent } from './pages/Chat/chat.component';
import { UsersListComponent } from './pages/HomeAdmin/UsersList/users-list.component';
import { UserDetailsComponent } from './pages/HomeAdmin/UsersDetails/user-details.component';
import { PlanningManagementTeacherComponent } from './pages/PlanningManagementTeacher/planning-management-teacher.component';
import { SearchForTeachersComponent } from './pages/SearchForTeachers/search-for-teachers.component';
import { TeacherProfileComponent } from './pages/TeacherProfile/teacher-profile.component';
import { HomeComponent } from './pages/Home/home.component';
import { DocumentsComponent } from './pages/Documents/documents.component';
import { PaymentsComponent } from './pages/Payments/payments.component';
import { teacherOnlyGuard, verifiedTeacherGuard } from './guards/verified-teacher.guard';

export const routes: Routes = [
  { path: '', component: HomeRouterComponent },
  { path: 'visio', component: PlanningComponent, canActivate: [verifiedTeacherGuard] },
  { 
    path: 'visio/:id', 
    component: Visio,
    data: { getPrerenderParams: () => [] },
    canActivate: [verifiedTeacherGuard]
  },
  { path: 'login', component: ConnexionComponent},
  { path: 'register', component: InscriptionComponent },
  { path: 'chat', component: ChatComponent, canActivate: [verifiedTeacherGuard] },
  { path: 'admin/users', component: UsersListComponent },
  { path: 'admin/users/:id', component: UserDetailsComponent },
  { path: 'teacher/planning', component: PlanningManagementTeacherComponent, canActivate: [verifiedTeacherGuard] },
  { path: 'search', component: SearchForTeachersComponent },
  { path: 'teachers/:id', component: TeacherProfileComponent },
  { path: 'home', component: HomeComponent },
  { path: 'documents', component: DocumentsComponent, canActivate: [teacherOnlyGuard] },
  { path: 'payments', component: PaymentsComponent },
  { path: '**', redirectTo: '' }
];


