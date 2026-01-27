import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { switchMap } from 'rxjs/operators';

// Services & Models
import { UserService } from '../../../services/user.service';
import { ProfileType, UserDto } from '../../../services/auth.service';

// Shared
import { HeaderComponent } from '../../../components/Header/header.component';

// Child Views (We will create these placeholders next)
import { StudentDetailComponent } from './student-detail/student-detail.component';
import { TeacherDetailComponent } from './teacher-detail/teacher-detail.component';
import { ParentDetailComponent } from './parent-detail/parent-detail.component';

@Component({
  selector: 'app-user-details',
  standalone: true,
  imports: [
    CommonModule,
    HeaderComponent,
    // Import the specific views
    StudentDetailComponent,
    TeacherDetailComponent,
    ParentDetailComponent
  ],
  templateUrl: './user-details.component.html',
  styleUrls: ['./user-details.component.scss']
})
export class UserDetailsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private userService = inject(UserService);

  // Make Enum available in template
  ProfileType = ProfileType;

  // The single source of truth for the user data
  user$: Observable<UserDto> | null = null;

  ngOnInit(): void {
    // 1. Listen to URL ID changes
    // 2. Fetch the user data accordingly
    this.user$ = this.route.paramMap.pipe(
      switchMap(params => {
        const id = params.get('id');
        if (!id) throw new Error('No ID provided');
        return this.userService.getUserById(id); // Ensure this exists in UserService
      })
    );
  }
}