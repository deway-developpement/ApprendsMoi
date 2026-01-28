import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom, forkJoin } from 'rxjs';
import { HeaderComponent } from '../../components/Header/header.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { AuthService, ProfileType, UserDto } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { environment } from '../../environments/environment';

interface AvailabilityResponse {
  id: string;
  teacherId: string;
  dayOfWeek: number;
  dayOfWeekName?: string | null;
  availabilityDate?: string | null;
  startTime: string;
  endTime: string;
  isRecurring: boolean;
}

interface UnavailableSlotResponse {
  id: string;
  teacherId: string;
  blockedDate: string;
  blockedStartTime: string;
  blockedEndTime: string;
  reason?: string | null;
  createdAt: string;
}

interface CreateAvailabilityRequest {
  dayOfWeek?: number | null;
  availabilityDate?: string | null;
  startTime: string;
  endTime: string;
  isRecurring: boolean;
}

interface CalendarDay {
  date: Date;
  label: string;
  dateLabel: string;
  key: string;
  dayOfWeek: number;
}

interface TimeSlot {
  startMinutes: number;
  endMinutes: number;
  label: string;
  rangeLabel: string;
  startTime: string;
  endTime: string;
}

interface SummarySlot {
  key: string;
  date: Date;
  startTime: string;
  endTime: string;
}

@Component({
  selector: 'app-planning-management-teacher',
  standalone: true,
  imports: [CommonModule, HeaderComponent, ButtonComponent],
  templateUrl: './planning-management-teacher.component.html',
  styleUrls: ['./planning-management-teacher.component.scss']
})
export class PlanningManagementTeacherComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly apiBaseUrl = `${environment.apiUrl}/api/availabilities`;

  weekDays: CalendarDay[] = [];
  timeSlots: TimeSlot[] = [];
  summarySlots: SummarySlot[] = [];

  weekRangeLabel = '';
  timeZoneLabel = '';

  isLoading = false;
  isSaving = false;

  private teacherId: string | null = null;
  private weekStart: Date = new Date();
  private weekEnd: Date = new Date();
  private readonly selectedSlotKeys = new Set<string>();
  private readonly pendingSlotKeys = new Set<string>();
  private readonly slotDetails = new Map<string, SummarySlot>();
  private readonly slotAvailabilityIds = new Map<string, string>();
  private readonly bookedSlotKeys = new Set<string>();

  async ngOnInit(): Promise<void> {
    this.buildCalendar();
    await this.loadTeacher();
    if (this.teacherId) {
      await this.loadAvailabilities();
    }
  }

  async reload(): Promise<void> {
    if (!this.teacherId) return;
    await this.loadAvailabilities();
  }

  isSlotActive(day: CalendarDay, slot: TimeSlot): boolean {
    return this.selectedSlotKeys.has(this.buildSlotKey(day.key, slot.startTime));
  }

  isSlotPending(day: CalendarDay, slot: TimeSlot): boolean {
    return this.pendingSlotKeys.has(this.buildSlotKey(day.key, slot.startTime));
  }

  isSlotBooked(day: CalendarDay, slot: TimeSlot): boolean {
    return this.bookedSlotKeys.has(this.buildSlotKey(day.key, slot.startTime));
  }

  isSlotPast(day: CalendarDay, slot: TimeSlot): boolean {
    const now = new Date();
    const slotDate = new Date(day.date);
    const [hour, minute] = slot.startTime.split(':').map((part) => Number(part));
    slotDate.setHours(hour, minute || 0, 0, 0);
    return slotDate < now;
  }

  isDayInPast(day: CalendarDay): boolean {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const date = new Date(day.date);
    date.setHours(0, 0, 0, 0);
    return date < today;
  }

  async toggleAvailability(day: CalendarDay, slot: TimeSlot): Promise<void> {
    if (this.isSlotPending(day, slot) || this.isLoading) return;

    if (this.isSlotBooked(day, slot)) return;

    if (this.isSlotActive(day, slot)) {
      await this.removeAvailability(day, slot);
    } else {
      await this.addAvailability(day, slot);
    }
  }

  async addAvailability(day: CalendarDay, slot: TimeSlot): Promise<void> {
    const key = this.buildSlotKey(day.key, slot.startTime);

    if (this.selectedSlotKeys.has(key)) {
      this.toastService.info('This slot is already saved.');
      return;
    }

    if (this.pendingSlotKeys.has(key) || this.isSaving) {
      return;
    }

    if (!this.teacherId) {
      this.toastService.error('You must be logged in as a teacher to add availability.');
      return;
    }

    const payload: CreateAvailabilityRequest = {
      dayOfWeek: day.dayOfWeek,
      availabilityDate: day.key,
      startTime: slot.startTime,
      endTime: slot.endTime,
      isRecurring: false
    };

    this.isSaving = true;
    this.pendingSlotKeys.add(key);

    try {
      const created = await firstValueFrom(
        this.http.post<AvailabilityResponse>(this.apiBaseUrl, payload)
      );
      this.applyAvailabilityToSlots(created);
      this.toastService.success(`Saved: ${day.label} ${day.dateLabel} ${slot.rangeLabel}.`);
      this.refreshSummary();
    } catch (err) {
      this.toastService.error(this.getErrorMessage(err, 'Unable to save availability.'));
    } finally {
      this.pendingSlotKeys.delete(key);
      this.isSaving = false;
    }
  }

  private async loadTeacher(): Promise<void> {
    let user: UserDto | null = null;

    user = await firstValueFrom(this.authService.currentUser$);
    if (!user) {
      try {
        user = await firstValueFrom(this.authService.fetchMe());
      } catch (err) {
        this.toastService.error(this.getErrorMessage(err, 'Unable to load user profile.'));
        return;
      }
    }

    if (!user) {
      this.toastService.error('You must be logged in to access this page.');
      return;
    }

    if (user.profileType !== ProfileType.Teacher) {
      this.toastService.warning('This page is only available for teachers.');
      return;
    }

    this.teacherId = user.id;
  }

  private async loadAvailabilities(): Promise<void> {
    if (!this.teacherId) return;

    this.isLoading = true;

    try {
      const { availabilities, blockedSlots } = await firstValueFrom(
        forkJoin({
          availabilities: this.http.get<AvailabilityResponse[]>(`${this.apiBaseUrl}/teacher/${this.teacherId}`),
          blockedSlots: this.http.get<UnavailableSlotResponse[]>(`${this.apiBaseUrl}/block/teacher/${this.teacherId}`)
        })
      );
      this.selectedSlotKeys.clear();
      this.slotDetails.clear();
      this.slotAvailabilityIds.clear();
      this.bookedSlotKeys.clear();
      this.applyAvailabilities(availabilities ?? []);
      this.applyBlockedSlots(blockedSlots ?? []);
      this.refreshSummary();
    } catch (err) {
      this.toastService.error(this.getErrorMessage(err, 'Unable to load availabilities.'));
    } finally {
      this.isLoading = false;
    }
  }

  private applyAvailabilities(availabilities: AvailabilityResponse[]): void {
    availabilities.forEach((availability) => {
      this.applyAvailabilityToSlots(availability);
    });
  }

  private applyBlockedSlots(blockedSlots: UnavailableSlotResponse[]): void {
    blockedSlots.forEach((blocked) => {
      const blockedDate = new Date(blocked.blockedDate);
      const dateKey = this.toDateKey(blockedDate);
      const startMinutes = this.timeToMinutes(blocked.blockedStartTime);
      const endMinutes = this.timeToMinutes(blocked.blockedEndTime);

      this.timeSlots.forEach((slot) => {
        if (slot.startMinutes >= startMinutes && slot.endMinutes <= endMinutes) {
          const key = this.buildSlotKey(dateKey, slot.startTime);
          this.bookedSlotKeys.add(key);
        }
      });
    });
  }

  private applyAvailabilityToSlots(availability: AvailabilityResponse): void {
    const dates = this.resolveAvailabilityDates(availability);
    if (!dates.length) return;

    const startMinutes = this.timeToMinutes(availability.startTime);
    const endMinutes = this.timeToMinutes(availability.endTime);

    dates.forEach((date) => {
      this.timeSlots.forEach((slot) => {
        if (slot.startMinutes >= startMinutes && slot.endMinutes <= endMinutes) {
          const key = this.buildSlotKey(this.toDateKey(date), slot.startTime);
          this.selectedSlotKeys.add(key);
          this.slotAvailabilityIds.set(key, availability.id);
          this.slotDetails.set(key, {
            key,
            date,
            startTime: slot.startTime,
            endTime: slot.endTime
          });
        }
      });
    });
  }

  private async removeAvailability(day: CalendarDay, slot: TimeSlot): Promise<void> {
    const key = this.buildSlotKey(day.key, slot.startTime);
    const availabilityId = this.slotAvailabilityIds.get(key);

    if (!availabilityId) {
      this.toastService.error('Unable to find the availability to remove.');
      return;
    }

    this.isSaving = true;
    this.pendingSlotKeys.add(key);

    try {
      await firstValueFrom(this.http.delete(`${this.apiBaseUrl}/${availabilityId}`));
      this.removeAvailabilityFromSlots(availabilityId);
      this.toastService.success(`Removed: ${day.label} ${day.dateLabel} ${slot.rangeLabel}.`);
      this.refreshSummary();
    } catch (err) {
      this.toastService.error(this.getErrorMessage(err, 'Unable to remove availability.'));
    } finally {
      this.pendingSlotKeys.delete(key);
      this.isSaving = false;
    }
  }

  private removeAvailabilityFromSlots(availabilityId: string): void {
    const keysToRemove: string[] = [];

    this.slotAvailabilityIds.forEach((id, key) => {
      if (id === availabilityId) {
        keysToRemove.push(key);
      }
    });

    keysToRemove.forEach((key) => {
      this.slotAvailabilityIds.delete(key);
      this.selectedSlotKeys.delete(key);
      this.slotDetails.delete(key);
    });
  }

  private resolveAvailabilityDates(availability: AvailabilityResponse): Date[] {
    if (availability.availabilityDate) {
      const date = this.parseDateOnly(availability.availabilityDate);
      if (date >= this.weekStart && date <= this.weekEnd) {
        return [date];
      }
      return [];
    }

    if (!availability.isRecurring) return [];

    return this.weekDays
      .filter((day) => day.dayOfWeek === availability.dayOfWeek)
      .map((day) => new Date(day.date));
  }

  private refreshSummary(): void {
    this.summarySlots = Array.from(this.slotDetails.values()).sort((a, b) => {
      const dateDiff = a.date.getTime() - b.date.getTime();
      if (dateDiff !== 0) return dateDiff;
      return this.timeToMinutes(a.startTime) - this.timeToMinutes(b.startTime);
    });
  }

  private buildCalendar(): void {
    this.weekStart = this.getStartOfCurrentWeek(new Date());
    this.weekEnd = this.addDays(this.weekStart, 6);

    this.weekDays = Array.from({ length: 7 }, (_, index) => {
      const date = this.addDays(this.weekStart, index);
      const label = date.toLocaleDateString('en-US', { weekday: 'short' });
      const dateLabel = date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
      return {
        date,
        label,
        dateLabel,
        key: this.toDateKey(date),
        dayOfWeek: date.getDay()
      };
    });

    this.timeSlots = Array.from({ length: 12 }, (_, index) => {
      const hour = 8 + index;
      const startTime = this.formatTime(hour, 0);
      const endTime = this.formatTime(hour + 1, 0);
      const label = `${this.pad(hour)}:00`;
      return {
        startMinutes: hour * 60,
        endMinutes: (hour + 1) * 60,
        label,
        rangeLabel: `${label} - ${this.pad(hour + 1)}:00`,
        startTime,
        endTime
      };
    });

    this.weekRangeLabel = `${this.formatShortDate(this.weekStart)} - ${this.formatShortDate(this.weekEnd)}`;
    const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone || 'Local time';
    this.timeZoneLabel = `Timezone: ${timeZone}`;
  }

  private getStartOfCurrentWeek(baseDate: Date): Date {
    const date = new Date(baseDate);
    date.setHours(0, 0, 0, 0);
    const day = date.getDay();
    const daysSinceMonday = (day + 6) % 7;
    return this.addDays(date, -daysSinceMonday);
  }

  private addDays(date: Date, days: number): Date {
    const next = new Date(date);
    next.setDate(next.getDate() + days);
    return next;
  }

  private toDateKey(date: Date): string {
    const year = date.getFullYear();
    const month = this.pad(date.getMonth() + 1);
    const day = this.pad(date.getDate());
    return `${year}-${month}-${day}`;
  }

  private parseDateOnly(value: string): Date {
    const [year, month, day] = value.split('-').map((part) => Number(part));
    return new Date(year, month - 1, day);
  }

  private formatShortDate(date: Date): string {
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }

  private formatTime(hour: number, minute: number): string {
    return `${this.pad(hour)}:${this.pad(minute)}:00`;
  }

  private pad(value: number): string {
    return value.toString().padStart(2, '0');
  }

  private buildSlotKey(dateKey: string, startTime: string): string {
    return `${dateKey}|${startTime}`;
  }

  private timeToMinutes(time: string): number {
    const [hour, minute] = time.split(':').map((part) => Number(part));
    return hour * 60 + (minute || 0);
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
