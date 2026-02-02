import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { HeaderComponent } from '../../components/Header/header.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { SmallIconComponent } from '../../components/shared/SmallIcon/small-icon.component';
import { CoursesScheduleComponent, Course } from '../../components/shared/CoursesSchedule/courses-schedule.component';
import { AuthService, UserDto } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { environment } from '../../environments/environment';
import { RouterLink } from "@angular/router";
import { RouterModule, Router } from '@angular/router';
import { TeacherReviewsComponent } from '../../components/shared/TeacherReviews/teacher-reviews.component';
import { IconComponent } from '../../components/shared/Icon/icon.component';

interface BookingRequest {
  id: number;
  courseId: string;
  parentName: string;
  subject: string;
  date: Date;
}

interface CourseDto {
  id: string;
  teacherId: string;
  teacherName: string;
  studentId: string;
  studentName: string;
  subjectId: string;
  subjectName: string;
  status: string;
  format: string;
  startDate: string;
  endDate: string;
  durationMinutes: number;
  priceSnapshot: number;
  commissionSnapshot: number;
  meetingLink?: string | null;
  studentAttended: boolean;
  attendanceMarkedAt?: string | null;
  createdAt: string;
}

interface MeetingResponse {
  id: number;
  meetingId: number;
  topic?: string | null;
  createdAt: string;
  scheduledStartTime?: string | null;
  duration: number;
  teacherId: string;
  studentId: string;
}

@Component({
  selector: 'app-home-teacher',
  templateUrl: './home-teacher.component.html',
  styleUrls: ['./home-teacher.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    HeaderComponent,
    ButtonComponent,
    SmallIconComponent,
    CoursesScheduleComponent,
    RouterLink,
    RouterModule,
    TeacherReviewsComponent,
    IconComponent
    
]
})
export class HomeTeacherComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly apiBaseUrl = `${environment.apiUrl}/api/zoom`;
  private readonly coursesBaseUrl = `${environment.apiUrl}/api/courses`;
  private readonly usersBaseUrl = `${environment.apiUrl}/api/Users`;
  private readonly userCache = new Map<string, UserDto>();

  teacherName = 'Professeur';
  currentUserId: string | null = null;
  isVerified = false;
  verificationStatus: 0 | 1 | 2 | 3 | undefined = 0;

  // Data
  nextCourse: Course | null = null;
  courses: Course[] = [];
  pendingRequests: BookingRequest[] = [];

  // KPI Data
  currentMonthRevenue: number = 0;
  pendingRevenue: number = 0;

  async ngOnInit(): Promise<void> {
    await this.loadUser();
    await this.loadMeetings();
    if (this.currentUserId) {
      await this.loadPendingCourses();
    }
  }

  private async loadUser(): Promise<void> {
    let user: UserDto | null = null;

    user = await firstValueFrom(this.authService.currentUser$);
    if (!user) {
      try {
        user = await firstValueFrom(this.authService.fetchMe());
      } catch (err) {
        this.toastService.error(this.getErrorMessage(err, 'Impossible de charger l\'utilisateur.'));
        return;
      }
    }

    if (!user) {
      return;
    }

    this.currentUserId = user.id;
    this.userCache.set(user.id, user);
    // Accept both VERIFIED (1) and DIPLOMA_VERIFIED (3)
    this.isVerified = user.verificationStatus === 1 || user.verificationStatus === 3;
    const displayName = this.formatUserName(user);
    if (displayName) {
      this.teacherName = displayName;
    }
    this.verificationStatus = user.verificationStatus;
  }

  private async loadMeetings(): Promise<void> {
    try {
      const meetings = await firstValueFrom(
        this.http.get<MeetingResponse[]>(`${this.apiBaseUrl}/meetings`)
      );

      const courses = await Promise.all(
        (meetings ?? []).map(async (meeting) => {
          const dateValue = meeting.scheduledStartTime ?? meeting.createdAt;
          const courseDate = this.parseUtcDate(dateValue);
          const safeDate = Number.isNaN(courseDate.getTime()) ? new Date() : courseDate;
          const isFuture = safeDate > new Date();
          const childName = await this.getUserName(meeting.studentId, 'Eleve');
          const subject = meeting.topic?.trim() || 'Session';

          return {
            id: meeting.id,
            date: safeDate,
            tutorName: this.teacherName || 'Moi',
            childName,
            subject,
            mode: 'Visio',
            status: isFuture ? 'Confirmé' : 'Terminé',
            price: 0
          } as Course;
        })
      );

      this.courses = courses.sort((a, b) => a.date.getTime() - b.date.getTime());
      this.nextCourse =
        this.courses.find(c => c.status === 'Confirmé' && c.date > new Date()) ?? null;

      this.calculateRevenue();
    } catch (err) {
      this.toastService.error(this.getErrorMessage(err, 'Impossible de charger les rendez-vous.'));
    }
  }

  private async loadPendingCourses(): Promise<void> {
    if (!this.currentUserId) return;

    try {
      const courses = await firstValueFrom(
        this.http.get<CourseDto[]>(`${this.coursesBaseUrl}/teacher/${this.currentUserId}`)
      );

      const pendingCourses = courses.filter(c => c.status === 'PENDING');

      this.pendingRequests = await Promise.all(
        pendingCourses.map(async (course) => {
          const parentName = await this.getUserName(course.studentId, 'Parent');
          const courseDate = this.parseUtcDate(course.startDate);

          return {
            id: parseInt(course.id.substring(0, 8), 16),
            courseId: course.id,
            parentName,
            subject: course.subjectName,
            date: courseDate
          } as BookingRequest;
        })
      );
    } catch (err) {
      this.toastService.error(this.getErrorMessage(err, 'Impossible de charger les demandes en attente.'));
    }
  }

  private async getUserName(userId: string, fallback: string): Promise<string> {
    if (!userId) {
      return fallback;
    }

    const cached = this.userCache.get(userId);
    if (cached) {
      return this.formatUserName(cached) || fallback;
    }

    try {
      const user = await firstValueFrom(this.http.get<UserDto>(`${this.usersBaseUrl}/${userId}`));
      this.userCache.set(userId, user);
      return this.formatUserName(user) || fallback;
    } catch (err) {
      return fallback;
    }
  }

  private formatUserName(user: UserDto): string {
    const fullName = `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim();
    if (fullName) {
      return fullName;
    }
    return user.username || '';
  }

  private calculateRevenue(): void {
    this.currentMonthRevenue = this.courses
      .filter(c => c.status === 'Terminé')
      .reduce((acc, curr) => acc + curr.price, 0);

    this.pendingRevenue = this.courses
      .filter(c => c.status === 'Confirmé')
      .reduce((acc, curr) => acc + curr.price, 0);
  }

  openVisio(course: Course): void {
    if (!course?.id) {
      this.toastService.warning('Visio indisponible pour ce cours.');
      return;
    }
    if (course.mode !== 'Visio') {
      this.toastService.info('Ce cours ne se fait pas en visio.');
      return;
    }
    this.router.navigate(['/visio', course.id]);
  }

  async acceptRequest(request: BookingRequest): Promise<void> {
    try {
      await firstValueFrom(
        this.http.put(`${this.coursesBaseUrl}/${request.courseId}`, {
          status: 'CONFIRMED'
        })
      );
      
      this.toastService.success('Demande acceptée avec succès');
      await this.loadPendingCourses();
      await this.loadMeetings();
    } catch (err) {
      this.toastService.error(this.getErrorMessage(err, 'Impossible d\'accepter la demande.'));
    }
  }

  async rejectRequest(request: BookingRequest): Promise<void> {
    try {
      await firstValueFrom(
        this.http.delete(`${this.coursesBaseUrl}/${request.courseId}`)
      );
      
      this.toastService.success('Demande refusée');
      await this.loadPendingCourses();
    } catch (err) {
      this.toastService.error(this.getErrorMessage(err, 'Impossible de refuser la demande.'));
    }
  }

  private parseUtcDate(dateString: string | null | undefined): Date {
    if (!dateString) {
      return new Date(NaN);
    }
    const hasTimeZone = /[zZ]|[+-]\d{2}:\d{2}$/.test(dateString);
    const normalized = hasTimeZone ? dateString : `${dateString}Z`;
    return new Date(normalized);
  }

  private getErrorMessage(err: unknown, fallback: string): string {
    if (err instanceof HttpErrorResponse) {
      if (typeof err.error === 'string') return err.error;
      if (err.error?.error) return err.error.error;
      return err.message || fallback;
    }

    if (err instanceof Error) return err.message;
    return fallback;
  }
}
