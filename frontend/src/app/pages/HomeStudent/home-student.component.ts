import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { HeaderComponent } from '../../components/Header/header.component';
import { SmallIconComponent } from '../../components/shared/SmallIcon/small-icon.component';
import { CoursesScheduleComponent, Course } from '../../components/shared/CoursesSchedule/courses-schedule.component';
import { AuthService, UserDto } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { environment } from '../../environments/environment';

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

interface StudentStatsDto {
  totalHoursThisMonth: number;
  numberOfCoursesThisMonth: number;
}

interface StatsResponseDto {
  userType?: string | null;
  stats?: Record<string, unknown> | null;
}

@Component({
  selector: 'app-home-student',
  templateUrl: './home-student.component.html',
  styleUrls: ['./home-student.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    HeaderComponent,
    SmallIconComponent,
    CoursesScheduleComponent
  ]
})
export class HomeStudentComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly apiBaseUrl = `${environment.apiUrl}/api/zoom`;
  private readonly statsUrl = `${environment.apiUrl}/api/stats`;
  private readonly usersBaseUrl = `${environment.apiUrl}/api/Users`;
  private readonly userCache = new Map<string, UserDto>();

  userName = 'Eleve';
  currentUserId: string | null = null;

  // Real backend data
  courses: Course[] = [];
  totalHoursThisMonth = 0;
  numberOfCoursesThisMonth = 0;

  async ngOnInit(): Promise<void> {
    await this.loadUser();
    await Promise.all([
      this.loadStudentStats(),
      this.loadMeetings()
    ]);
  }

  get totalHoursThisMonthLabel(): string {
    return new Intl.NumberFormat('fr-FR', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 2
    }).format(this.totalHoursThisMonth);
  }

  get numberOfCoursesThisMonthLabel(): string {
    return new Intl.NumberFormat('fr-FR').format(this.numberOfCoursesThisMonth);
  }

  private async loadUser(): Promise<void> {
    let user: UserDto | null = null;

    user = await firstValueFrom(this.authService.currentUser$);
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
    const displayName = this.formatUserName(user);
    if (displayName) {
      this.userName = displayName;
    }
  }

  private async loadStudentStats(): Promise<void> {
    try {
      const response = await firstValueFrom(this.http.get<StatsResponseDto>(this.statsUrl));
      const stats = response?.stats;

      if (!stats) {
        this.totalHoursThisMonth = 0;
        this.numberOfCoursesThisMonth = 0;
        return;
      }

      const parsed = this.extractStudentStats(stats);
      this.totalHoursThisMonth = parsed.totalHoursThisMonth;
      this.numberOfCoursesThisMonth = parsed.numberOfCoursesThisMonth;
    } catch (err) {
      console.error('Error loading student stats:', err);
      this.totalHoursThisMonth = 0;
      this.numberOfCoursesThisMonth = 0;
      this.toastService.error('Impossible de charger les statistiques etudiant.');
    }
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
          const tutorName = await this.getUserName(meeting.teacherId, 'Professeur');
          const subject = meeting.topic?.trim() || 'Session';

          return {
            id: meeting.id,
            date: safeDate,
            tutorName,
            subject,
            childName: this.userName || 'Moi',
            mode: 'Visio',
            status: isFuture ? 'Confirmé' : 'Terminé',
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

  private extractStudentStats(stats: Record<string, unknown>): StudentStatsDto {
    return {
      totalHoursThisMonth: this.toNumber(stats['totalHoursThisMonth'] ?? stats['TotalHoursThisMonth']),
      numberOfCoursesThisMonth: this.toNumber(stats['numberOfCoursesThisMonth'] ?? stats['NumberOfCoursesThisMonth'])
    };
  }

  private toNumber(value: unknown): number {
    const numericValue = Number(value);
    return Number.isFinite(numericValue) ? numericValue : 0;
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
}
