import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Observable } from 'rxjs';

// Services & Models
import { AuthService, UserDto, ProfileType } from '../../services/auth.service';

// Child Components (The different "Homes")
import { HomeComponent } from '../Home/home.component';               // Public Landing Page
import { HomeStudentComponent } from '../HomeStudent/home-student.component'; // Student Home
import { HomeTeacherComponent } from '../HomeTeacher/home-teacher.component'; // Teacher Home
import { HomeAdminComponent } from '../HomeAdmin/home-admin.component';
import { HomeParentComponent } from '../HomeParent/home-parent.component';

@Component({
  selector: 'app-home-router',
  standalone: true,
  imports: [
    CommonModule,
    HomeComponent,
    HomeStudentComponent,
    HomeTeacherComponent,
    HomeAdminComponent,
    HomeParentComponent,
],
  templateUrl: './home-router.component.html',
  styleUrls: ['./home-router.component.scss']
})
export class HomeRouterComponent implements OnInit {
  currentUser$: Observable<UserDto | null>;
  
  // Expose the Enum to the HTML template
  ProfileType = ProfileType;

  constructor(private authService: AuthService) {
    this.currentUser$ = this.authService.currentUser$;
  }

  ngOnInit(): void {
    // Logic is handled reactively via the async pipe in HTML
  }
}