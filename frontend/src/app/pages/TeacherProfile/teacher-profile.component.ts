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
import { SubjectService } from '../../services/subject.service';
import { IconComponent } from '../../components/shared/Icon/icon.component';
import { 
  TeacherBookingService, 
  CalendarDay, 
  TimeSlot, 
  AvailabilityResponse, 
  SubjectDto,
  CourseFormat 
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
  verificationStatus?: number;
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
    TeacherReviewsComponent,
    IconComponent,
  ],
  templateUrl: './teacher-profile.component.html',
  styleUrls: ['./teacher-profile.component.scss']
})
export class TeacherProfileComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly parentService = inject(ParentService);
  private readonly toastService = inject(ToastService);
  private readonly bookingService = inject(TeacherBookingService);
  private readonly subjectService = inject(SubjectService);
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
    this.loadSubjects();
  }

  get canBook(): boolean {
    return this.userProfile === ProfileType.Parent;
  }

  get needsStudentSelection(): boolean {
    return this.userProfile === ProfileType.Parent;
  }

  private loadSubjects(): void {
    if (!this.teacherId) return;

    this.subjectService.getTeacherSubjects(this.teacherId).subscribe({
      next: (teacherSubjects) => {
        this.subjectOptions = teacherSubjects.map(ts => ({
          label: `${ts.subjectName} (${ts.pricePerHour}€/h)`,
          value: ts.subjectId
        }));
        if (this.subjectOptions.length > 0) {
          this.selectedSubjectId = this.subjectOptions[0].value;
        }
      },
      error: () => this.toastService.error('Impossible de charger les matières de ce professeur.')
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

        // --- MODIFICATION ICI : INVERSER L'ORDRE ---
        // 1. On marque d'abord ce qui est réservé (Orange)
        this.applyExistingCourses(res.existingCourses); 
        
        // 2. On calcule les dispos ensuite (Bleu)
        this.applyAvailabilities(res.availabilities);
        // -------------------------------------------

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
  if (!studentId || !this.selectedSubjectId) {
    this.toastService.warning('Veuillez sélectionner un élève et une matière.');
    return;
  }

  this.isBooking = true;
  try {
    // On construit manuellement la date sans passer par l'objet Date pour éviter le décalage UTC
    // Format attendu : "YYYY-MM-DDTHH:mm:00"
    const startDateRaw = `${this.bookingDate}T${this.bookingTime}:00.000Z`;

    await firstValueFrom(
      this.bookingService.createCourse({
        teacherId: this.teacherId!,
        studentId: String(studentId),
        subjectId: String(this.selectedSubjectId),
        startDate: startDateRaw,
        durationMinutes: 60,
        format: CourseFormat.VISIO 
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
    if (!this.isSlotAvailable(day, slot) || this.isSlotBooked(day, slot) || this.isSlotPast(day, slot)) return;
    console.log("Selecting slot:", { day, slot });
    this.selectedSlotKey = this.bookingService.buildSlotKey(day.key, slot.startTime);
    this.selectedSlotLabel = `${day.label} ${day.dateLabel}`;
    this.selectedSlotRange = slot.rangeLabel;
    this.bookingDate = day.key;
    this.bookingTime = slot.startTime.slice(0, 5);
    console.log("bookingTime set to:", this.bookingTime);
  }

  isSlotAvailable = (day: CalendarDay, slot: TimeSlot) => {
    const key = this.bookingService.buildSlotKey(day.key, slot.startTime);
    return this.availableSlotKeys.has(key) && !this.bookedSlotKeys.has(key);
  };
  isSlotBooked = (day: CalendarDay, slot: TimeSlot) => {
    const key = this.bookingService.buildSlotKey(day.key, slot.startTime);
    const isBooked = this.bookedSlotKeys.has(key);
    return isBooked;
  };  isSlotSelected = (day: CalendarDay, slot: TimeSlot) => this.selectedSlotKey === this.bookingService.buildSlotKey(day.key, slot.startTime);
  isSlotPast = (day: CalendarDay, slot: TimeSlot) => this.bookingService.isSlotInPast(day, slot);

  private applyExistingCourses(courses: any[]): void {
    if (!courses || courses.length === 0) {
      console.log('❌ Pas de cours existants');
      return;
    }

    courses.forEach((course, idx) => {
      // ✅ Extraire l'heure directement de la chaîne ISO
      // Format ISO : "2026-01-29T08:00:00Z" ou "2026-01-29T08:00:00"
      const isoString = course.startDate;
      const timePart = isoString.split('T')[1]; // "08:00:00Z"
      const hours = timePart.split(':')[0]; // "08"
      const minutes = timePart.split(':')[1]; // "00"
      
      console.log(`Course ${idx}:`, {
        rawStartDate: course.startDate,
        extractedHours: hours,
        extractedMinutes: minutes
      });

      // La date au format YYYY-MM-DD
      const dateKey = isoString.split('T')[0]; // "2026-01-29"
      const startTime = `${hours}:${minutes}:00`;
      const key = this.bookingService.buildSlotKey(dateKey, startTime);
      
      console.log(`  → Slot clé générée: ${key}`);
      this.bookedSlotKeys.add(key);
    });
    
    console.log('📋 Tous les booked slot keys:', Array.from(this.bookedSlotKeys));
  }
    
  private applyAvailabilities(availabilities: AvailabilityResponse[]): void {
    console.log('📅 Availabilities reçus:', availabilities);
    
    availabilities.forEach((av) => {
      const startMin = this.bookingService.timeToMinutes(av.startTime);
      const endMin = this.bookingService.timeToMinutes(av.endTime);

      this.weekDays.forEach((day) => {
        let isMatch = false;
        if (av.availabilityDate) {
          if (av.availabilityDate.split('T')[0] === day.key) isMatch = true;
        } else if (av.isRecurring && day.dayOfWeek === av.dayOfWeek) {
          isMatch = true;
        }

        if (isMatch) {
          this.timeSlots.forEach((slot) => {
            if (slot.startMinutes >= startMin && slot.endMinutes <= endMin) {
              const key = this.bookingService.buildSlotKey(day.key, slot.startTime);
              
              if (this.bookedSlotKeys.has(key)) {
              } else {
                this.availableSlotKeys.add(key);
              }
            }
          });
        }
      });
    });

  }

  private resolveStudentId = () => this.userProfile === ProfileType.Student ? this.currentUserId : (this.selectedStudentId ? String(this.selectedStudentId) : null);

  private async loadUser(): Promise<void> {
    let user = await firstValueFrom(this.authService.currentUser$);
    if (!user) try { user = await firstValueFrom(this.authService.fetchMe()); } catch { return; }
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
    const radius = dto.travelRadiusKm ?? 0;
    return {
      id: dto.id, name: `${dto.firstName} ${dto.lastName}`.trim() || 'Professeur',
      city: dto.city || 'Ville inconnue', headline: dto.bio || 'Profil en cours de mise à jour.',
      rating: 4.8, reviews: 20, pricePerHour: 30, subjects: ['Toutes matières'], levels: ['Tous niveaux'],
      bio: dto.bio || 'Profil en cours de mise à jour.', specialties: ['Suivi personnalisé', 'Révision', 'Méthodologie'],
      highlights: [{ label: 'Élèves', value: '80+' }, { label: 'Réponse', value: '< 2h' }, { label: 'Cours', value: '300+' }],
      availability: [{ label: 'Lun', time: '18:00', format: radius > 0 ? 'Domicile' : 'Visio' }],
      languages: ['Français'], education: ['Enseignant'], certifications: ['En cours'],
      avatarColor: dto.isPremium ? '#fbbf24' : '#1a365d', isPremium: dto.isPremium, isVerified: dto.verificationStatus === 1, isTop: dto.isPremium,
      verificationStatus: dto.verificationStatus
    };
  }

  resetBooking(): void {
    this.bookingDate = ''; this.bookingTime = ''; this.bookingTopic = ''; this.selectedSlotKey = null;
    this.selectedSlotLabel = ''; this.selectedSlotRange = '';
  }

  private getErrorMessage(err: unknown, fallback: string): string {
    if (err instanceof HttpErrorResponse) return err.error?.error || err.message || fallback;
    return err instanceof Error ? err.message : fallback;
  }
}