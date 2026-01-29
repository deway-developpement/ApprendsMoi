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

interface Message {
  sender: string;
  preview: string;
  date: Date;
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
  selector: 'app-home-student',
  templateUrl: './home-student.component.html',
  styleUrls: ['./home-student.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    HeaderComponent,
    ButtonComponent,
    SmallIconComponent,
    CoursesScheduleComponent
  ]
})
export class HomeStudentComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly apiBaseUrl = `${environment.apiUrl}/api/zoom`;
  private readonly usersBaseUrl = `${environment.apiUrl}/api/Users`;
  private readonly userCache = new Map<string, UserDto>();

  userName = 'Eleve';
  currentUserId: string | null = null;

  // Data
  nextCourse: Course | null = null;
  lastMessage: Message | null = null;
  courses: Course[] = [];

  async ngOnInit(): Promise<void> {
    await this.loadUser();
    await this.loadMeetings();

    this.lastMessage = {
      sender: 'Julie B.',
      preview: 'N\'oublie pas de faire l\'exercice 3 page 12 pour demain !',
      date: new Date()
    };
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
    const displayName = this.formatUserName(user);
    if (displayName) {
      this.userName = displayName;
    }
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
      this.nextCourse =
        this.courses.find(c => c.status === 'Confirmé' && c.date > new Date()) ?? null;
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
