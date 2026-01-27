import { Routes } from '@angular/router';
import { Visio } from './pages/Visio/Visio.component';
import { PlanningComponent } from './pages/Visio/Planning/planning.component';
import { ConnexionComponent } from './pages/Connexion/connexion.component';
import { InscriptionComponent } from './pages/Inscription/inscription.component';
import { HomeRouterComponent } from './pages/HomeRooter/home-router.component';
import { PlanningManagementTeacherComponent } from './pages/PlanningManagementTeacher/planning-management-teacher.component';
import { SearchForTeachersComponent } from './pages/SearchForTeachers/search-for-teachers.component';
import { TeacherProfileComponent } from './pages/TeacherProfile/teacher-profile.component';

export const routes: Routes = [
  { path: '', component: HomeRouterComponent },
  { path: 'visio', component: PlanningComponent },
  { path: 'visio/:id', component: Visio },
  { path: 'login', component: ConnexionComponent},
  { path: 'register', component: InscriptionComponent },
  { path: 'teacher/planning', component: PlanningManagementTeacherComponent },
  { path: 'search', component: SearchForTeachersComponent },
  { path: 'teachers/:id', component: TeacherProfileComponent }
];
