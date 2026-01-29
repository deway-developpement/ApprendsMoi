import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { forkJoin, Observable } from 'rxjs';
import { environment } from '../environments/environment';


export enum CourseFormat {
  HOME = "HOME", // ou 'Home' selon ton backend
  VISIO = "VISIO"
}
// --- Interfaces exportées ---

export interface CalendarDay {
  date: Date;
  label: string;
  dateLabel: string;
  key: string;
  dayOfWeek: number;
}

export interface TimeSlot {
  startMinutes: number;
  endMinutes: number;
  label: string;
  rangeLabel: string;
  startTime: string;
  endTime: string;
}

export interface AvailabilityResponse {
  id: string;
  teacherId: string;
  dayOfWeek: number;
  dayOfWeekName?: string | null;
  availabilityDate?: string | null;
  startTime: string;
  endTime: string;
  isRecurring: boolean;
}

export interface UnavailableSlotResponse {
  id: string;
  teacherId: string;
  blockedDate: string;
  blockedStartTime: string;
  blockedEndTime: string;
  reason?: string | null;
  createdAt: string;
}

export interface CourseDto {
  id: string;
  teacherId: string;
  studentId: string;
  subjectId: string;
  startDate: string;
  endDate: string;
  durationMinutes: number;
  status: string;
}

// Nouvelle interface pour les matières
export interface SubjectDto {
  id: string;
  name: string;
  slug: string;
}

@Injectable({
  providedIn: 'root'
})
export class TeacherBookingService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  /**
   * Récupère la liste des matières disponibles
   */
  getSubjects(): Observable<SubjectDto[]> {
    return this.http.get<SubjectDto[]>(`${this.apiUrl}/api/Subjects`);
  }

  /**
   * Récupère les données du profil, les disponibilités, les blocs manuels 
   * et les cours existants pour identifier les créneaux occupés.
   */
  getTeacherBookingData(teacherId: string) {
    return forkJoin({
      profile: this.http.get<any>(`${this.apiUrl}/api/Users/${teacherId}`),
      availabilities: this.http.get<AvailabilityResponse[]>(`${this.apiUrl}/api/availabilities/teacher/${teacherId}`),
      existingCourses: this.http.get<CourseDto[]>(`${this.apiUrl}/api/Courses/teacher/${teacherId}`)
    });
  }

  /**
   * Crée un nouveau cours
   */
  createCourse(payload: {
    teacherId: string;
    studentId: string;
    subjectId: string; 
    startDate: string;
    durationMinutes: number;
    format: CourseFormat; 
  }): Observable<CourseDto> {
    return this.http.post<CourseDto>(`${this.apiUrl}/api/Courses`, payload);
  }

  /**
   * Génère la structure des 7 jours de la semaine à partir d'une date
   */
  buildCalendarDays(baseDate: Date): CalendarDay[] {
    const startOfWeek = this.getStartOfCurrentWeek(baseDate);
    return Array.from({ length: 7 }, (_, index) => {
      const date = this.addDays(startOfWeek, index);
      // ✅ BUILD KEY FROM LOCAL DATE, NO ISO CONVERSION
      const year = date.getFullYear();
      const month = this.pad(date.getMonth() + 1);
      const day = this.pad(date.getDate());
      const key = `${year}-${month}-${day}`;
      return {
        date,
        label: date.toLocaleDateString('fr-FR', { weekday: 'short' }),
        dateLabel: date.toLocaleDateString('fr-FR', { month: 'short', day: 'numeric' }),
        key,
        dayOfWeek: date.getDay()
      };
    });
  }

  /**
   * Génère les créneaux horaires fixes (ex: 8h à 20h)
   */
  generateTimeSlots(startHour: number = 8, count: number = 12): TimeSlot[] {
    return Array.from({ length: count }, (_, index) => {
      const hour = startHour + index;
      const startTime = `${this.pad(hour)}:00:00`;
      const label = `${this.pad(hour)}:00`;
      return {
        startMinutes: hour * 60,
        endMinutes: (hour + 1) * 60,
        label,
        rangeLabel: `${label} - ${this.pad(hour + 1)}:00`,
        startTime,
        endTime: `${this.pad(hour + 1)}:00:00`
      };
    });
  }

  /**
   * Vérifie si un créneau est dans le passé
   */
  isSlotInPast(day: CalendarDay, slot: TimeSlot): boolean {
    const now = new Date();
    const slotDate = new Date(day.date);
    const [hour, minute] = slot.startTime.split(':').map(Number);
    slotDate.setHours(hour, minute || 0, 0, 0);
    return slotDate < now;
  }

  // --- Helpers utilitaires ---

  getStartOfCurrentWeek(baseDate: Date): Date {
    const date = new Date(baseDate);
    date.setHours(0, 0, 0, 0);
    const day = date.getDay();
    const daysSinceMonday = (day + 6) % 7;
    return this.addDays(date, -daysSinceMonday);
  }

  addDays(date: Date, days: number): Date {
    const next = new Date(date);
    next.setDate(next.getDate() + days);
    return next;
  }

  toDateKey(dateOrString: Date | string): string {
    if (typeof dateOrString === 'string') {
      return dateOrString.split('T')[0]; // Prend juste la date du string
    }
    const year = dateOrString.getFullYear();
    const month = this.pad(dateOrString.getMonth() + 1);
    const day = this.pad(dateOrString.getDate());
    return `${year}-${month}-${day}`;
  }

  pad(value: number): string {
    return value.toString().padStart(2, '0');
  }

  timeToMinutes(time: string): number {
    if (!time) return 0;
    const [hour, minute] = time.split(':').map(Number);
    return hour * 60 + (minute || 0);
  }

  buildSlotKey(dateKey: string, startTime: string): string {
    const formattedTime = startTime.length === 5 ? `${startTime}:00` : startTime;
    return `${dateKey}|${formattedTime}`;
  }
}