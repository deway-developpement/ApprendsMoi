import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom, forkJoin } from 'rxjs';

import { HeaderComponent } from '../../components/Header/header.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { SmallIconComponent } from '../../components/shared/SmallIcon/small-icon.component';
import { SelectComponent, SelectOption } from '../../components/shared/Select/select.component';
import { TextInputComponent } from '../../components/shared/TextInput/text-input.component';
import { AuthService, ProfileType, UserDto } from '../../services/auth.service';
import { ParentService, Child } from '../../services/parent.service';
import { ToastService } from '../../services/toast.service';
import { environment } from '../../environments/environment';

interface HighlightStat {
  label: string;
  value: string;
}

interface AvailabilitySlot {
  label: string;
  time: string;
  format: string;
}

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

interface TeacherProfile {
  id: string;
  name: string;
  city: string;
  headline: string;
  rating: number;
  reviews: number;
  pricePerHour: number;
  subjects: string[];
  levels: string[];
  bio: string;
  specialties: string[];
  highlights: HighlightStat[];
  availability: AvailabilitySlot[];
  languages: string[];
  education: string[];
  certifications: string[];
  avatarColor: string;
  isPremium: boolean;
  isVerified: boolean;
  isTop: boolean;
}

interface CreateMeetingResponse {
  id: number;
}

@Component({
  selector: 'app-teacher-profile',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    HeaderComponent,
    ButtonComponent,
    SmallIconComponent,
    SelectComponent,
    TextInputComponent
  ],
  templateUrl: './teacher-profile.component.html',
  styleUrls: ['./teacher-profile.component.scss']
})
export class TeacherProfileComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly parentService = inject(ParentService);
  private readonly toastService = inject(ToastService);
  private readonly apiBaseUrl = `${environment.apiUrl}/api/zoom`;

  teacher: TeacherProfile | null = null;
  teacherId: string | null = null;

  userProfile: ProfileType | null = null;
  currentUserId: string | null = null;

  bookingDate = '';
  bookingTime = '';
  bookingTopic = '';
  bookingDuration: string | number | null = 60;
  isBooking = false;

  availabilityLoading = false;
  weekDays: CalendarDay[] = [];
  timeSlots: TimeSlot[] = [];
  selectedSlotKey: string | null = null;
  selectedSlotLabel = '';
  selectedSlotRange = '';

  private readonly availableSlotKeys = new Set<string>();
  private readonly bookedSlotKeys = new Set<string>();

  private weekStart: Date = new Date();
  private weekEnd: Date = new Date();

  studentOptions: SelectOption[] = [];
  selectedStudentId: string | number | null = null;

  durationOptions: SelectOption[] = [
    { label: '30 min', value: 30 },
    { label: '45 min', value: 45 },
    { label: '60 min', value: 60 },
    { label: '90 min', value: 90 }
  ];

  private readonly teachers: TeacherProfile[] = [
    {
      id: '019bfc6d-893d-782a-87db-da528377e1b6',
      name: 'Camille Robert',
      city: 'Paris',
      headline: 'Maths & Physique • 12 ans d experience • Approche methodique',
      rating: 4.9,
      reviews: 56,
      pricePerHour: 32,
      subjects: ['Mathematiques', 'Physique-Chimie'],
      levels: ['Troisieme', 'Seconde', 'Premiere'],
      bio: 'Je propose un accompagnement structure, des fiches de revision claires et des exercices adaptes a chaque eleve.',
      specialties: ['Preparation brevet', 'Methodologie', 'Confiance en soi'],
      highlights: [
        { label: 'Students helped', value: '180+' },
        { label: 'Response time', value: '< 1h' },
        { label: 'Lessons completed', value: '900+' }
      ],
      availability: [
        { label: 'Tue', time: '17:00 - 18:00', format: 'Visio' },
        { label: 'Wed', time: '14:00 - 15:00', format: 'Domicile' },
        { label: 'Sat', time: '10:00 - 11:00', format: 'Hybride' }
      ],
      languages: ['Francais', 'Anglais'],
      education: ['Master Physique - Sorbonne', 'CAPES Mathematiques'],
      certifications: ['Education nationale', 'Coach scolaire certifie'],
      avatarColor: '#1a365d',
      isPremium: true,
      isVerified: true,
      isTop: false
    },
    {
      id: '019bfc6d-8a81-792f-a9e9-6129d53afd7f',
      name: 'Noah Petit',
      city: 'Lyon',
      headline: 'Anglais conversationnel • 8 ans d experience • Visio',
      rating: 4.7,
      reviews: 41,
      pricePerHour: 26,
      subjects: ['Anglais'],
      levels: ['Quatrieme', 'Troisieme', 'Seconde'],
      bio: 'Cours interactifs, preparation examens et prise de parole pour progresser rapidement.',
      specialties: ['Oral', 'Toeic', 'Revision rapide'],
      highlights: [
        { label: 'Students helped', value: '120+' },
        { label: 'Response time', value: '< 2h' },
        { label: 'Lessons completed', value: '540+' }
      ],
      availability: [
        { label: 'Mon', time: '18:00 - 19:00', format: 'Visio' },
        { label: 'Thu', time: '19:00 - 20:00', format: 'Visio' }
      ],
      languages: ['Francais', 'Anglais'],
      education: ['Licence Langues etrangeres appliquees'],
      certifications: ['Cambridge C2'],
      avatarColor: '#f97316',
      isPremium: false,
      isVerified: true,
      isTop: false
    },
    {
      id: '019bfc70-9e81-7d6e-aad5-77797653a390',
      name: 'Lea Benali',
      city: 'Bordeaux',
      headline: 'Francais & Histoire-Geo • Pedagogie creative • Domicile',
      rating: 5.0,
      reviews: 22,
      pricePerHour: 30,
      subjects: ['Francais', 'Histoire-Geographie'],
      levels: ['Sixieme', 'Cinquieme', 'Quatrieme'],
      bio: 'Cours vivants, suivi personnalise et methodes simples pour progresser.',
      specialties: ['Expression ecrite', 'Methodologie', 'Confiance'],
      highlights: [
        { label: 'Students helped', value: '90+' },
        { label: 'Response time', value: '< 2h' },
        { label: 'Lessons completed', value: '420+' }
      ],
      availability: [
        { label: 'Tue', time: '16:00 - 17:00', format: 'Domicile' },
        { label: 'Fri', time: '18:00 - 19:00', format: 'Hybride' }
      ],
      languages: ['Francais', 'Anglais'],
      education: ['Master Lettres modernes'],
      certifications: ['Certification FLE'],
      avatarColor: '#4EE381',
      isPremium: true,
      isVerified: true,
      isTop: true
    }
  ];

  constructor(private route: ActivatedRoute) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.teacherId = idParam;
    this.teacher = this.teachers.find(item => item.id === idParam) ?? this.teachers[0];
    if (!this.teacherId) {
      this.teacherId = this.teacher?.id ?? null;
    }
    this.buildCalendar();
    if (this.teacherId) {
      this.loadAvailabilities();
    }
    this.loadUser();
  }

  get canBook(): boolean {
    return this.userProfile === ProfileType.Parent || this.userProfile === ProfileType.Student;
  }

  get needsStudentSelection(): boolean {
    return this.userProfile === ProfileType.Parent;
  }

  async bookSession(): Promise<void> {
    if (!this.canBook) {
      this.toastService.warning('Booking is available for parents or students.');
      return;
    }

    if (!this.teacherId) {
      this.toastService.error('Teacher id is missing.');
      return;
    }

    if (!this.selectedSlotKey || !this.bookingDate || !this.bookingTime) {
      this.toastService.warning('Please select a slot from the calendar.');
      return;
    }

    const studentId = this.resolveStudentId();
    if (!studentId) {
      this.toastService.warning('Please select a student.');
      return;
    }

    const scheduledDate = new Date(`${this.bookingDate}T${this.bookingTime}:00`);
    if (Number.isNaN(scheduledDate.getTime())) {
      this.toastService.error('Invalid date or time.');
      return;
    }

    const duration = Number(this.bookingDuration) || 60;
    const topic = this.bookingTopic?.trim() || `Session with ${this.teacher?.name ?? 'teacher'}`;

    this.isBooking = true;

    try {
      await firstValueFrom(
        this.http.post<CreateMeetingResponse>(`${this.apiBaseUrl}/meeting`, {
          topic,
          teacherId: this.teacherId,
          studentId,
          time: scheduledDate.toISOString(),
          duration
        })
      );

      this.toastService.success('Appointment booked successfully.');
      this.resetBooking();
    } catch (err) {
      this.toastService.error(this.getErrorMessage(err, 'Unable to book appointment.'));
    } finally {
      this.isBooking = false;
    }
  }

  resetBooking(): void {
    this.bookingDate = '';
    this.bookingTime = '';
    this.bookingTopic = '';
    this.bookingDuration = 60;
    this.selectedSlotKey = null;
    this.selectedSlotLabel = '';
    this.selectedSlotRange = '';
    if (this.userProfile !== ProfileType.Student) {
      this.selectedStudentId = null;
    }
  }

  private resolveStudentId(): string | null {
    if (this.userProfile === ProfileType.Student) {
      return this.currentUserId;
    }

    if (this.userProfile === ProfileType.Parent) {
      return this.selectedStudentId ? String(this.selectedStudentId) : null;
    }

    return null;
  }

  private async loadUser(): Promise<void> {
    let user: UserDto | null = null;

    user = await firstValueFrom(this.authService.currentUser$);
    if (!user) {
      try {
        user = await firstValueFrom(this.authService.fetchMe());
      } catch (err) {
        this.toastService.error(this.getErrorMessage(err, 'Unable to load user.'));
        return;
      }
    }

    if (!user) {
      return;
    }

    this.userProfile = user.profileType;
    this.currentUserId = user.id;

    if (this.userProfile === ProfileType.Parent) {
      this.loadChildren();
    }

    if (this.userProfile === ProfileType.Student) {
      this.selectedStudentId = user.id;
    }
  }

  private loadChildren(): void {
    this.parentService.getMyChildren().subscribe({
      next: (children: Child[]) => {
        this.studentOptions = children.map(child => ({
          label: `${child.firstName} ${child.lastName}`,
          value: child.id
        }));
        if (!this.selectedStudentId && this.studentOptions.length > 0) {
          this.selectedStudentId = this.studentOptions[0].value;
        }
      },
      error: (err) => {
        this.toastService.error(this.getErrorMessage(err, 'Unable to load students.'));
      }
    });
  }

  async loadAvailabilities(): Promise<void> {
    if (!this.teacherId) return;

    this.availableSlotKeys.clear();
    this.bookedSlotKeys.clear();
    this.availabilityLoading = true;

    try {
      const { availabilities, blockedSlots } = await firstValueFrom(
        forkJoin({
          availabilities: this.http.get<AvailabilityResponse[]>(`${environment.apiUrl}/api/availabilities/teacher/${this.teacherId}`),
          blockedSlots: this.http.get<UnavailableSlotResponse[]>(`${environment.apiUrl}/api/availabilities/block/teacher/${this.teacherId}`)
        })
      );

      this.applyAvailabilities(availabilities ?? []);
      this.applyBlockedSlots(blockedSlots ?? []);
    } catch (err) {
      this.toastService.error(this.getErrorMessage(err, 'Unable to load availabilities.'));
    } finally {
      this.availabilityLoading = false;
    }
  }

  selectSlot(day: CalendarDay, slot: TimeSlot): void {
    if (!this.isSlotAvailable(day, slot) || this.isSlotBooked(day, slot) || this.isSlotPast(day, slot)) {
      return;
    }

    this.selectedSlotKey = this.buildSlotKey(day.key, slot.startTime);
    this.selectedSlotLabel = `${day.label} ${day.dateLabel}`;
    this.selectedSlotRange = slot.rangeLabel;
    this.bookingDate = day.key;
    this.bookingTime = slot.startTime.slice(0, 5);
    this.bookingDuration = 60;
  }

  isSlotAvailable(day: CalendarDay, slot: TimeSlot): boolean {
    return this.availableSlotKeys.has(this.buildSlotKey(day.key, slot.startTime));
  }

  isSlotBooked(day: CalendarDay, slot: TimeSlot): boolean {
    return this.bookedSlotKeys.has(this.buildSlotKey(day.key, slot.startTime));
  }

  isSlotSelected(day: CalendarDay, slot: TimeSlot): boolean {
    return this.selectedSlotKey === this.buildSlotKey(day.key, slot.startTime);
  }

  isSlotPast(day: CalendarDay, slot: TimeSlot): boolean {
    const now = new Date();
    const slotDate = new Date(day.date);
    const [hour, minute] = slot.startTime.split(':').map((part) => Number(part));
    slotDate.setHours(hour, minute || 0, 0, 0);
    return slotDate < now;
  }

  private applyAvailabilities(availabilities: AvailabilityResponse[]): void {
    availabilities.forEach((availability) => {
      const dates = this.resolveAvailabilityDates(availability);
      if (!dates.length) return;

      const startMinutes = this.timeToMinutes(availability.startTime);
      const endMinutes = this.timeToMinutes(availability.endTime);

      dates.forEach((date) => {
        this.timeSlots.forEach((slot) => {
          if (slot.startMinutes >= startMinutes && slot.endMinutes <= endMinutes) {
            const key = this.buildSlotKey(this.toDateKey(date), slot.startTime);
            this.availableSlotKeys.add(key);
          }
        });
      });
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
