import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { HeaderComponent } from '../../components/Header/header.component';
import { SmallIconComponent } from '../../components/shared/SmallIcon/small-icon.component';
import { CoursesScheduleComponent, Course } from '../../components/shared/CoursesSchedule/courses-schedule.component';
import { AuthService, UserDto } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { environment } from '../../environments/environment';
import { TeacherReviewsComponent } from '../../components/shared/TeacherReviews/teacher-reviews.component';
import { IconComponent } from '../../components/shared/Icon/icon.component';

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

interface TeacherStatsDto {
  averageRating: number | null;
  numberOfReviewers: number;
  earningsThisMonth: number;
  currentStudentsFollowing: number;
}

interface StatsResponseDto {
  userType?: string | null;
  stats?: Record<string, unknown> | null;
}

@Component({
  selector: 'app-home-teacher',
  templateUrl: './home-teacher.component.html',
  styleUrls: ['./home-teacher.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    HeaderComponent,
    SmallIconComponent,
    CoursesScheduleComponent,
    RouterLink,
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
  private readonly statsUrl = `${environment.apiUrl}/api/stats`;
  private readonly usersBaseUrl = `${environment.apiUrl}/api/Users`;
  private readonly userCache = new Map<string, UserDto>();
  private readonly numberFormatter = new Intl.NumberFormat('fr-FR');
  private readonly currencyFormatter = new Intl.NumberFormat('fr-FR', {
    style: 'currency',
    currency: 'EUR',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });

  teacherName = 'Professeur';
  currentUserId: string | null = null;
  isVerified = false;
  verificationStatus: 0 | 1 | 2 | 3 | undefined = 0;

  // Real backend data
  courses: Course[] = [];
  teacherStats: TeacherStatsDto = this.createDefaultStats();

  async ngOnInit(): Promise<void> {
    await this.loadUser();
    await Promise.all([
      this.loadTeacherStats(),
      this.loadMeetings()
    ]);
  }

  get averageRatingLabel(): string {
    if (this.teacherStats.averageRating === null) {
      return '-';
    }

    return new Intl.NumberFormat('fr-FR', {
      minimumFractionDigits: 1,
      maximumFractionDigits: 1
    }).format(this.teacherStats.averageRating);
  }

  get numberOfReviewersLabel(): string {
    return this.numberFormatter.format(this.teacherStats.numberOfReviewers);
  }

  get earningsThisMonthLabel(): string {
    return this.currencyFormatter.format(this.teacherStats.earningsThisMonth);
  }

  get currentStudentsFollowingLabel(): string {
    return this.numberFormatter.format(this.teacherStats.currentStudentsFollowing);
  }

  private createDefaultStats(): TeacherStatsDto {
    return {
      averageRating: null,
      numberOfReviewers: 0,
      earningsThisMonth: 0,
      currentStudentsFollowing: 0
    };
  }

  private async loadUser(): Promise<void> {
    let user: UserDto | null = await firstValueFrom(this.authService.currentUser$);

    if (!user) {
      try {
        user = await firstValueFrom(this.authService.fetchMe());
      } catch (err) {
        this.toastService.error(this.getErrorMessage(err, "Impossible de charger l'utilisateur."));
        return;
      }
    }

    if (!user) {
      return;
    }

    this.currentUserId = user.id;
    this.userCache.set(user.id, user);
    this.isVerified = user.verificationStatus === 1 || user.verificationStatus === 3;
    this.verificationStatus = user.verificationStatus;

    const displayName = this.formatUserName(user);
    if (displayName) {
      this.teacherName = displayName;
    }
  }

  private async loadTeacherStats(): Promise<void> {
    try {
      const response = await firstValueFrom(
        this.http.get<StatsResponseDto>(this.statsUrl)
      );

      const stats = response?.stats;
      if (!stats) {
        this.teacherStats = this.createDefaultStats();
        return;
      }

      this.teacherStats = this.extractTeacherStats(stats);
    } catch (err) {
      console.error('Error loading teacher stats:', err);
      this.teacherStats = this.createDefaultStats();
      this.toastService.error('Impossible de charger les statistiques enseignant.');
    }
  }

  private extractTeacherStats(stats: Record<string, unknown>): TeacherStatsDto {
    return {
      averageRating: this.toNullableNumber(stats['averageRating'] ?? stats['AverageRating']),
      numberOfReviewers: this.toNumber(stats['numberOfReviewers'] ?? stats['NumberOfReviewers']),
      earningsThisMonth: this.toNumber(stats['earningsThisMonth'] ?? stats['EarningsThisMonth']),
      currentStudentsFollowing: this.toNumber(stats['currentStudentsFollowing'] ?? stats['CurrentStudentsFollowing'])
    };
  }

  private toNullableNumber(value: unknown): number | null {
    if (value === null || value === undefined) {
      return null;
    }

    const numericValue = Number(value);
    return Number.isFinite(numericValue) ? numericValue : null;
  }

  private toNumber(value: unknown): number {
    const numericValue = Number(value);
    return Number.isFinite(numericValue) ? numericValue : 0;
  }

  private async loadMeetings(): Promise<void> {
    try {
      const meetings = await firstValueFrom(
        this.http.get<MeetingResponse[]>(`${this.apiBaseUrl}/meetings`)
      );

      const courses = await Promise.all(
        (meetings ?? []).map(async meeting => {
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
            status: (isFuture ? 'Confirmé' : 'Terminé') as Course['status'],
            price: 0
          } as Course;
        })
      );

      this.courses = courses.sort((a, b) => a.date.getTime() - b.date.getTime());
    } catch (err) {
      this.toastService.error(this.getErrorMessage(err, 'Impossible de charger les rendez-vous.'));
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
    } catch {
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
