import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';

// Components
import { HeaderComponent } from '../../components/Header/header.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { IconComponent } from '../../components/shared/Icon/icon.component';
import { SmallIconComponent } from '../../components/shared/SmallIcon/small-icon.component';
import { IconButtonComponent } from '../../components/shared/IconButton/icon-button.component';
import { TextInputComponent } from '../../components/shared/TextInput/text-input.component';
import { SelectComponent, SelectOption } from '../../components/shared/Select/select.component';
import { CoursesScheduleComponent, Course } from '../../components/shared/CoursesSchedule/courses-schedule.component';

// Services & Models
import { ParentService, Child, CreateChildRequest } from '../../services/parent.service';
import { AuthService, GradeLevel, UserDto } from '../../services/auth.service';
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

interface ParentStatsDto {
  totalDebt: number;
  coursesBookedThisMonth: number;
  numberOfChildren: number;
}

interface StatsResponseDto {
  userType?: string | null;
  stats?: Record<string, unknown> | null;
}

@Component({
  selector: 'app-home-parent',
  templateUrl: './home-parent.component.html',
  styleUrls: ['./home-parent.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    HeaderComponent,
    ButtonComponent,
    IconComponent,
    SmallIconComponent,
    IconButtonComponent,
    TextInputComponent,
    SelectComponent,
    CoursesScheduleComponent
  ]
})
export class HomeParentComponent implements OnInit {
  // Injections
  private readonly parentService = inject(ParentService);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);

  private readonly statsUrl = `${environment.apiUrl}/api/stats`;
  private readonly meetingsUrl = `${environment.apiUrl}/api/zoom/meetings`;
  private readonly usersBaseUrl = `${environment.apiUrl}/api/Users`;
  private readonly userCache = new Map<string, UserDto>();
  private readonly currencyFormatter = new Intl.NumberFormat('fr-FR', {
    style: 'currency',
    currency: 'EUR',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
  private readonly numberFormatter = new Intl.NumberFormat('fr-FR');

  // User state
  userName = '';
  userLastName = '';

  // UI state
  showChildModal = false;
  isLoading = false;

  // Real backend data
  children: Child[] = [];
  courses: Course[] = [];
  totalDebt = 0;
  coursesBookedThisMonth = 0;
  numberOfChildren = 0;

  // Form data
  newChildName = '';
  newChildLevel: string | number | null = null;
  newChildBirthDate = '';
  newChildPassword = '';

  selectedChild: Child | null = null;
  showDetailsModal = false;

  // Options
  levelOptions: SelectOption[] = [
    { label: 'CP', value: GradeLevel.CP },
    { label: 'CE1', value: GradeLevel.CE1 },
    { label: 'CE2', value: GradeLevel.CE2 },
    { label: 'CM1', value: GradeLevel.CM1 },
    { label: 'CM2', value: GradeLevel.CM2 },
    { label: '6eme', value: GradeLevel.Sixieme },
    { label: '5eme', value: GradeLevel.Cinquieme },
    { label: '4eme', value: GradeLevel.Quatrieme },
    { label: '3eme', value: GradeLevel.Troisieme },
    { label: 'Seconde', value: GradeLevel.Seconde },
    { label: 'Premiere', value: GradeLevel.Premiere },
    { label: 'Terminale', value: GradeLevel.Terminale }
  ];

  async ngOnInit(): Promise<void> {
    await this.loadUser();
    await this.refreshParentData();
  }

  get totalDebtFormatted(): string {
    return this.currencyFormatter.format(this.totalDebt);
  }

  get coursesBookedThisMonthFormatted(): string {
    return this.numberFormatter.format(this.coursesBookedThisMonth);
  }

  get numberOfChildrenFormatted(): string {
    return this.numberFormatter.format(this.numberOfChildren);
  }

  private async refreshParentData(): Promise<void> {
    await Promise.all([
      this.loadChildren(),
      this.loadParentStats(),
      this.loadMeetings()
    ]);
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

    this.userCache.set(user.id, user);
    this.userName = this.formatUserName(user);
    this.userLastName = user.lastName || '';
  }

  private async loadChildren(): Promise<void> {
    try {
      this.children = await firstValueFrom(this.parentService.getMyChildren());
    } catch (err) {
      console.error('Impossible de charger les enfants', err);
      this.toastService.error('Impossible de charger les enfants.');
      this.children = [];
    }
  }

  private async loadParentStats(): Promise<void> {
    try {
      const response = await firstValueFrom(this.http.get<StatsResponseDto>(this.statsUrl));
      const stats = response?.stats;

      if (!stats) {
        this.totalDebt = 0;
        this.coursesBookedThisMonth = 0;
        this.numberOfChildren = 0;
        return;
      }

      const parsedStats = this.extractParentStats(stats);
      this.totalDebt = parsedStats.totalDebt;
      this.coursesBookedThisMonth = parsedStats.coursesBookedThisMonth;
      this.numberOfChildren = parsedStats.numberOfChildren;
    } catch (err) {
      console.error('Error loading parent stats:', err);
      this.totalDebt = 0;
      this.coursesBookedThisMonth = 0;
      this.numberOfChildren = 0;
      this.toastService.error('Impossible de charger les statistiques parent.');
    }
  }

  private async loadMeetings(): Promise<void> {
    try {
      const meetings = await firstValueFrom(this.http.get<MeetingResponse[]>(this.meetingsUrl));

      const courses = await Promise.all(
        (meetings ?? []).map(async meeting => {
          const dateValue = meeting.scheduledStartTime ?? meeting.createdAt;
          const courseDate = this.parseUtcDate(dateValue);
          const safeDate = Number.isNaN(courseDate.getTime()) ? new Date() : courseDate;
          const isFuture = safeDate > new Date();
          const tutorName = await this.getUserName(meeting.teacherId, 'Professeur');
          const childName = await this.getUserName(meeting.studentId, 'Eleve');
          const subject = meeting.topic?.trim() || 'Session';

          return {
            id: meeting.id,
            date: safeDate,
            tutorName,
            subject,
            childName,
            mode: 'Visio',
            status: isFuture ? 'Confirmé' : 'Terminé',
            price: 0
          } as Course;
        })
      );

      this.courses = courses.sort((a, b) => a.date.getTime() - b.date.getTime());
    } catch (err) {
      console.error('Error loading meetings:', err);
      this.courses = [];
      this.toastService.error(this.getErrorMessage(err, 'Impossible de charger les cours.'));
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

  private extractParentStats(stats: Record<string, unknown>): ParentStatsDto {
    return {
      totalDebt: this.toNumber(stats['totalDebt'] ?? stats['TotalDebt']),
      coursesBookedThisMonth: this.toNumber(stats['coursesBookedThisMonth'] ?? stats['CoursesBookedThisMonth']),
      numberOfChildren: this.toNumber(stats['numberOfChildren'] ?? stats['NumberOfChildren'])
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

  openDetailsModal(child: Child): void {
    this.selectedChild = child;
    this.showDetailsModal = true;
  }

  closeDetailsModal(): void {
    this.showDetailsModal = false;
    this.selectedChild = null;
  }

  openChildModal(): void {
    this.showChildModal = true;
  }

  closeChildModal(): void {
    this.showChildModal = false;
    this.newChildName = '';
    this.newChildLevel = null;
    this.newChildBirthDate = '';
    this.newChildPassword = '';
  }

  private hasPasswordComplexity(password: string): boolean {
    if (password.length < 6) return false;
    const hasUpper = /[A-Z]/.test(password);
    const hasLower = /[a-z]/.test(password);
    const hasDigit = /\d/.test(password);
    return hasUpper && hasLower && hasDigit;
  }

  saveChild(): void {
    if (!this.newChildName || this.newChildLevel === null || !this.newChildPassword) {
      this.toastService.warning('Veuillez remplir les champs obligatoires.');
      return;
    }

    if (!this.hasPasswordComplexity(this.newChildPassword)) {
      this.toastService.warning('Le mot de passe doit contenir : 6 caracteres min, 1 majuscule, 1 minuscule, 1 chiffre.');
      return;
    }

    this.isLoading = true;

    const request: CreateChildRequest = {
      firstName: this.newChildName,
      lastName: this.userLastName || 'NomFamille',
      password: this.newChildPassword,
      gradeLevel: Number(this.newChildLevel),
      birthDate: this.newChildBirthDate || undefined
    };

    this.parentService.addChild(request).subscribe({
      next: () => {
        this.isLoading = false;
        this.toastService.success('Compte enfant cree avec succes !');
        this.closeChildModal();
        void this.refreshParentData();
      },
      error: err => {
        this.isLoading = false;
        console.error('Erreur inscription:', err);
        const message = err.error?.error || 'Erreur lors de la creation.';
        this.toastService.error(message);
      }
    });
  }

  deleteChild(id: string): void {
    if (confirm('Voulez-vous vraiment supprimer ce profil enfant ? Cette action est definitive.')) {
      this.parentService.deleteChild(id).subscribe({
        next: () => {
          this.toastService.success('Le profil enfant a ete supprime.');
          void this.refreshParentData();
        },
        error: err => {
          console.error('Erreur lors de la suppression :', err);
          const message = err.error?.detail || 'Impossible de supprimer ce profil.';
          this.toastService.error(message);
        }
      });
    }
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
