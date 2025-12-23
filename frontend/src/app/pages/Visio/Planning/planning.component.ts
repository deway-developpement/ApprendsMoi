import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { HeaderComponent } from '../../../components/Header/header.component';
import { ButtonComponent } from '../../../components/shared/Button/button.component';

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

@Component({
  standalone: true,
  selector: 'app-planning',
  templateUrl: './planning.component.html',
  styleUrls: ['./planning.component.scss'],
  imports: [CommonModule, HeaderComponent, ButtonComponent]
})
export class PlanningComponent implements OnInit {
  private readonly apiBaseUrl = 'http://localhost:5254/api/zoom';
  
  meetings: Meeting[] = [];
  isLoading = false;
  error = '';
  isCreating = false;

  constructor(private router: Router) {}

  async ngOnInit(): Promise<void> {
    await this.loadMeetings();
  }

  async loadMeetings(): Promise<void> {
    try {
      this.isLoading = true;
      this.error = '';

      const response = await fetch(`${this.apiBaseUrl}/meetings`);

      if (!response.ok) {
        throw new Error('Erreur lors du chargement des réunions');
      }

      this.meetings = await response.json();
      this.isLoading = false;
    } catch (err) {
      this.isLoading = false;
      this.error = err instanceof Error ? err.message : 'Erreur lors du chargement des réunions';
      console.error('Error loading meetings:', err);
    }
  }

  async createNewMeeting(): Promise<void> {
    try {
      this.isCreating = true;
      this.error = '';

      const response = await fetch(`${this.apiBaseUrl}/meeting`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ topic: 'ApprendsMoi - Session de classe' })
      });

      if (!response.ok) {
        throw new Error('Erreur lors de la création de la réunion');
      }

      const newMeeting = await response.json();
      this.isCreating = false;
      
      // Redirect to the newly created meeting
      this.router.navigate(['/visio', newMeeting.id]);
    } catch (err) {
      this.isCreating = false;
      this.error = err instanceof Error ? err.message : 'Erreur lors de la création de la réunion';
      console.error('Error creating meeting:', err);
    }
  }

  openMeeting(meetingId: number): void {
    this.router.navigate(['/visio', meetingId]);
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
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
    const scheduledTime = new Date(meeting.scheduledStartTime);
    return scheduledTime > new Date();
  }
}
