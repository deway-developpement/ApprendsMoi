import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { HeaderComponent } from '../../components/Header/header.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { SelectComponent, SelectOption } from '../../components/shared/Select/select.component';
import { TextInputComponent } from '../../components/shared/TextInput/text-input.component';
import { TeacherReviewsComponent } from '../../components/shared/TeacherReviews/teacher-reviews.component';

import { AuthService, ProfileType, UserDto } from '../../services/auth.service';
import { ParentService, Child } from '../../services/parent.service';
import { ToastService } from '../../services/toast.service';
import { 
  TeacherBookingService, 
  CalendarDay, 
  TimeSlot, 
  AvailabilityResponse, 
  UnavailableSlotResponse,
  SubjectDto // Ajout de l'interface
} from '../../services/teacher-booking.service';

interface HighlightStat {
  label: string;
  value: string;
}

interface AvailabilitySlot {
  label: string;
  time: string;
  format: string;
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

interface TeacherDto {
  id: string;
  firstName: string;
  lastName: string;
  profilePicture?: string | null;
  bio?: string | null;
  verificationStatus: number;
  isPremium: boolean;
  city?: string | null;
  travelRadiusKm?: number | null;
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
    SelectComponent,
    TextInputComponent,
    TeacherReviewsComponent
  ],
  templateUrl: './teacher-profile.component.html',
  styleUrls: ['./teacher-profile.component.scss']
})
export class TeacherProfileComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly parentService = inject(ParentService);
  private readonly toastService = inject(ToastService);
  private readonly bookingService = inject(TeacherBookingService);
  private readonly route = inject(ActivatedRoute);

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

  studentOptions: SelectOption[] = [];
  selectedStudentId: string | number | null = null;

  // --- Nouveautés pour les sujets ---
  subjectOptions: SelectOption[] = [];
  selectedSubjectId: string | number | null = null;

  durationOptions: SelectOption[] = [
    { label: '60 min', value: 60 }
  ];

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.teacherId = idParam;
    
    this.weekDays = this.bookingService.buildCalendarDays(new Date());
    this.timeSlots = this.bookingService.generateTimeSlots();

    if (this.teacherId) {
      this.loadTeacherData();
    }
    this.loadUser();
    this.loadSubjects(); // Chargement des matières au démarrage
  }

  get canBook(): boolean {
    return this.userProfile === ProfileType.Parent;
  }

  get needsStudentSelection(): boolean {
    return this.userProfile === ProfileType.Parent;
  }

  // --- Méthode pour charger les matières ---
  private loadSubjects(): void {
    this.bookingService.getSubjects().subscribe({
      next: (subjects: SubjectDto[]) => {
        this.subjectOptions = subjects.map(s => ({
          label: s.name,
          value: s.id
        }));
        // Sélection par défaut de la première matière si disponible
        if (this.subjectOptions.length > 0) {
          this.selectedSubjectId = this.subjectOptions[0].value;
        }
      },
      error: (err) => {
        this.toastService.error('Impossible de charger les matières.');
      }
    });
  }

  private loadTeacherData(): void {
    if (!this.teacherId) return;
    this.availabilityLoading = true;

    this.availableSlotKeys.clear();
    this.bookedSlotKeys.clear();

    this.bookingService.getTeacherBookingData(this.teacherId).subscribe({
      next: (res) => {
        this.teacher = this.mapTeacherProfile(res.profile);
        this.applyAvailabilities(res.availabilities);
        this.applyBlockedSlots(res.blockedSlots);
        this.applyExistingCourses(res.existingCourses); 
        this.availabilityLoading = false;
      },
      error: (err) => {
        this.toastService.error(this.getErrorMessage(err, 'Erreur de chargement.'));
        this.availabilityLoading = false;
      }
    });
  }

  async bookSession(): Promise<void> {
    if (!this.canBook) {
      this.toastService.warning('La réservation est disponible pour les parents.');
      return;
    }

    if (!this.teacherId || !this.selectedSlotKey || !this.bookingDate || !this.bookingTime) {
      this.toastService.warning('Veuillez sélectionner un créneau.');
      return;
    }

    const studentId = this.resolveStudentId();
    if (!studentId) {
      this.toastService.warning('Veuillez sélectionner un élève.');
      return;
    }

    if (!this.selectedSubjectId) {
      this.toastService.warning('Veuillez sélectionner une matière.');
      return;
    }

    this.isBooking = true;
    try {
      const scheduledDate = new Date(`${this.bookingDate}T${this.bookingTime}:00`);
      
      await firstValueFrom(
        this.bookingService.createCourse({
          teacherId: this.teacherId!,
          studentId: String(studentId),
          subjectId: String(this.selectedSubjectId), // Utilisation de la matière sélectionnée
          startDate: scheduledDate.toISOString(),
          durationMinutes: 60,
          format: "Online" 
        })
      );

      this.toastService.success('Cours réservé avec succès.');
      this.resetBooking();
      this.loadTeacherData();
    } catch (err) {
      this.toastService.error(this.getErrorMessage(err, 'Impossible de réserver le cours.'));
    } finally {
      this.isBooking = false;
    }
  }

  selectSlot(day: CalendarDay, slot: TimeSlot): void {
    if (!this.isSlotAvailable(day, slot) || this.isSlotBooked(day, slot) || this.isSlotPast(day, slot)) {
      return;
    }

    this.selectedSlotKey = this.bookingService.buildSlotKey(day.key, slot.startTime);
    this.selectedSlotLabel = `${day.label} ${day.dateLabel}`;
    this.selectedSlotRange = slot.rangeLabel;
    this.bookingDate = day.key;
    this.bookingTime = slot.startTime.slice(0, 5);
  }

  isSlotAvailable(day: CalendarDay, slot: TimeSlot): boolean {
    return this.availableSlotKeys.has(this.bookingService.buildSlotKey(day.key, slot.startTime));
  }

  isSlotBooked(day: CalendarDay, slot: TimeSlot): boolean {
    return this.bookedSlotKeys.has(this.bookingService.buildSlotKey(day.key, slot.startTime));
  }

  isSlotSelected(day: CalendarDay, slot: TimeSlot): boolean {
    return this.selectedSlotKey === this.bookingService.buildSlotKey(day.key, slot.startTime);
  }

  isSlotPast(day: CalendarDay, slot: TimeSlot): boolean {
    return this.bookingService.isSlotInPast(day, slot);
  }

  private applyExistingCourses(courses: any[]): void {
    if (!courses) return;
    
    courses.forEach(course => {
      const date = new Date(course.startDate);
      const dateKey = this.bookingService.toDateKey(date);
      
      const hours = this.bookingService.pad(date.getHours());
      const minutes = this.bookingService.pad(date.getMinutes());
      const startTime = `${hours}:${minutes}:00`; 
      
      const key = this.bookingService.buildSlotKey(dateKey, startTime);
      this.bookedSlotKeys.add(key);
    });
  }

  private applyAvailabilities(availabilities: AvailabilityResponse[]): void {
    availabilities.forEach((availability) => {
      const startMinutes = this.bookingService.timeToMinutes(availability.startTime);
      const endMinutes = this.bookingService.timeToMinutes(availability.endTime);

      this.weekDays.forEach((day) => {
        let isMatch = false;
        if (availability.availabilityDate) {
          if (availability.availabilityDate.split('T')[0] === day.key) isMatch = true;
        } else if (availability.isRecurring && day.dayOfWeek === availability.dayOfWeek) {
          isMatch = true;
        }

        if (isMatch) {
          this.timeSlots.forEach((slot) => {
            if (slot.startMinutes >= startMinutes && slot.endMinutes <= endMinutes) {
              this.availableSlotKeys.add(this.bookingService.buildSlotKey(day.key, slot.startTime));
            }
          });
        }
      });
    });
  }

  private applyBlockedSlots(blockedSlots: UnavailableSlotResponse[]): void {
    blockedSlots.forEach((blocked) => {
      const dateKey = blocked.blockedDate.split('T')[0];
      const startMinutes = this.bookingService.timeToMinutes(blocked.blockedStartTime);
      const endMinutes = this.bookingService.timeToMinutes(blocked.blockedEndTime);

      this.timeSlots.forEach((slot) => {
        if (slot.startMinutes >= startMinutes && slot.endMinutes <= endMinutes) {
          this.bookedSlotKeys.add(this.bookingService.buildSlotKey(dateKey, slot.startTime));
        }
      });
    });
  }

  private resolveStudentId(): string | null {
    if (this.userProfile === ProfileType.Student) return this.currentUserId;
    if (this.userProfile === ProfileType.Parent) return this.selectedStudentId ? String(this.selectedStudentId) : null;
    return null;
  }

  private async loadUser(): Promise<void> {
    let user = await firstValueFrom(this.authService.currentUser$);
    if (!user) {
      try { user = await firstValueFrom(this.authService.fetchMe()); } catch (err) { return; }
    }
    if (!user) return;
    this.userProfile = user.profileType;
    this.currentUserId = user.id;
    if (this.userProfile === ProfileType.Parent) this.loadChildren();
    if (this.userProfile === ProfileType.Student) this.selectedStudentId = user.id;
  }

  private loadChildren(): void {
    this.parentService.getMyChildren().subscribe({
      next: (children: Child[]) => {
        this.studentOptions = children.map(child => ({ label: `${child.firstName} ${child.lastName}`, value: child.id }));
        if (!this.selectedStudentId && this.studentOptions.length > 0) this.selectedStudentId = this.studentOptions[0].value;
      }
    });
  }

  private mapTeacherProfile(dto: TeacherDto): TeacherProfile {
    const travelRadius = dto.travelRadiusKm ?? 0;
    return {
      id: dto.id,
      name: `${dto.firstName} ${dto.lastName}`.trim() || 'Professeur',
      city: dto.city || 'Ville inconnue',
      headline: dto.bio || 'Profil en cours de mise à jour.',
      rating: 4.8, reviews: 20, pricePerHour: 30,
      subjects: ['Toutes matières'], levels: ['Tous niveaux'],
      bio: dto.bio || 'Profil en cours de mise à jour.',
      specialties: ['Suivi personnalisé', 'Révision', 'Méthodologie'],
      highlights: [{ label: 'Élèves', value: '80+' }, { label: 'Réponse', value: '< 2h' }, { label: 'Cours', value: '300+' }],
      availability: [{ label: 'Lun', time: '18:00', format: travelRadius > 0 ? 'Domicile' : 'Visio' }],
      languages: ['Français'], education: ['Enseignant'], certifications: ['En cours'],
      avatarColor: dto.isPremium ? '#fbbf24' : '#1a365d',
      isPremium: dto.isPremium, isVerified: dto.verificationStatus === 1, isTop: dto.isPremium
    };
  }

  resetBooking(): void {
    this.bookingDate = ''; this.bookingTime = ''; this.bookingTopic = ''; this.selectedSlotKey = null;
    this.selectedSlotLabel = ''; this.selectedSlotRange = '';
    // On ne reset pas la matière choisie par défaut pour fluidifier l'expérience
  }

  private getErrorMessage(err: unknown, fallback: string): string {
    if (err instanceof HttpErrorResponse) return err.error?.error || err.message || fallback;
    return err instanceof Error ? err.message : fallback;
  }
}