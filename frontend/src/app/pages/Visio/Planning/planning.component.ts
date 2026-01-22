import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { HeaderComponent } from '../../../components/Header/header.component';
import { ButtonComponent } from '../../../components/shared/Button/button.component';
import { environment } from '../../../environments/environment';

interface Meeting {
  id: number;
  zoomMeetingId: number;
  topic: string;
  joinUrl: string;
  startUrl: string;
  password: string;
  createdAt: string;
  scheduledStartTime: string | null;
  duration: number;
}

interface CreateMeetingResponse {
  id: number;
}

@Component({
  standalone: true,
  selector: 'app-planning',
  templateUrl: './planning.component.html',
  styleUrls: ['./planning.component.scss'],
  imports: [CommonModule, HeaderComponent, ButtonComponent]
})
export class PlanningComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = `${environment.apiUrl}/api/zoom`;
  
  meetings: Meeting[] = [];
  isLoading = false;
  error = '';
  isCreating = false;

  constructor(private router: Router) {}

  async ngOnInit(): Promise<void> {
    await this.loadMeetings();
  }

  async loadMeetings(): Promise<void> {
    this.isLoading = true;
    this.error = '';

    try {
      this.meetings = await firstValueFrom(
        this.http.get<Meeting[]>(`${this.apiBaseUrl}/meetings`)
      );
    } catch (err) {
      this.error = this.getErrorMessage(err, 'Erreur lors du chargement des réunions');
      console.error('Error loading meetings:', err);
    } finally {
      this.isLoading = false;
    }
  }

  async createNewMeeting(): Promise<void> {
    this.isCreating = true;
    this.error = '';

    try {
      const newMeeting = await firstValueFrom(
        this.http.post<CreateMeetingResponse>(`${this.apiBaseUrl}/meeting`, {
          topic: 'ApprendsMoi - Session de classe',
          teacherId: 2,
          studentId: 3
        })
      );

      // Redirect to the newly created meeting
      this.router.navigate(['/visio', newMeeting.id]);
    } catch (err) {
      this.error = this.getErrorMessage(err, 'Erreur lors de la création de la réunion');
      console.error('Error creating meeting:', err);
    } finally {
      this.isCreating = false;
    }
  }

  openMeeting(meetingId: number): void {
    this.router.navigate(['/visio', meetingId]);
  }

  formatDate(dateString: string): string {
    const date = this.parseUtcDate(dateString);
    if (Number.isNaN(date.getTime())) {
      return dateString;
    }
    return date.toLocaleString('fr-FR', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  isFutureMeeting(meeting: Meeting): boolean {
    if (!meeting.scheduledStartTime) return true;
    const scheduledTime = this.parseUtcDate(meeting.scheduledStartTime);
    return scheduledTime > new Date();
  }

  copyMeetingLink(meeting: Meeting, event: Event): void {
    event.stopPropagation();
    const link = `${window.location.origin}/visio/${meeting.id}`;
    navigator.clipboard.writeText(link).then(() => {
      alert('Lien copié dans le presse-papiers!');
    });
  }

  copyZoomLink(meeting: Meeting, event: Event): void {
    event.stopPropagation();
    navigator.clipboard.writeText(meeting.joinUrl).then(() => {
      alert('Lien Zoom copié dans le presse-papiers!');
    });
  }

  openInZoom(meeting: Meeting, event: Event): void {
    event.stopPropagation();
    window.open(meeting.joinUrl, '_blank', 'noopener,noreferrer');
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

  private parseUtcDate(dateString: string): Date {
    const hasTimeZone = /[zZ]|[+-]\d{2}:\d{2}$/.test(dateString);
    const normalized = hasTimeZone ? dateString : `${dateString}Z`;
    return new Date(normalized);
  }
}

