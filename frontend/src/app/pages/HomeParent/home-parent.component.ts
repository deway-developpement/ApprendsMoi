import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';

// Components
import { HeaderComponent } from '../../components/Header/header.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { IconComponent } from '../../components/shared/Icon/icon.component';
import { SmallIconComponent } from '../../components/shared/SmallIcon/small-icon.component';
import { IconButtonComponent } from '../../components/shared/IconButton/icon-button.component';
import { TextInputComponent } from '../../components/shared/TextInput/text-input.component';
import { SelectComponent, SelectOption } from '../../components/shared/Select/select.component';
import { CoursesScheduleComponent } from '../../components/shared/CoursesSchedule/courses-schedule.component';

// Services & Models
import { ParentService, Child, CreateChildRequest } from '../../services/parent.service';
import { AuthService, GradeLevel, UserDto } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';

// Interfaces locales
interface Course {
  id: number;
  date: Date;
  tutorName: string;
  subject: string;
  childName: string;
  mode: 'Domicile' | 'Visio';
  status: 'Confirmé' | 'En attente' | 'Annulé' | 'Terminé';
  price: number;
}

interface Message {
  sender: string;
  preview: string;
  date: Date;
}

interface Payment {
  amount: number;
  status: 'Payé' | 'En attente';
  date: Date;
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
  private parentService = inject(ParentService);
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private router = inject(Router);

  // État de l'utilisateur
  userName = ''; 
  userLastName = '';
  userId: string | null = null; // ID stocké proprement ici
  
  // État de l'interface
  currentTab: 'upcoming' | 'history' = 'upcoming';
  showChildModal = false;
  isLoading = false;

  // Données
  children: Child[] = [];
  courses: Course[] = [];
  nextCourse: Course | null = null;
  lastMessage: Message | null = null;
  lastPayment: Payment | null = null;

  // Données du Formulaire
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
    { label: '6ème', value: GradeLevel.Sixieme },
    { label: '5ème', value: GradeLevel.Cinquieme },
    { label: '4ème', value: GradeLevel.Quatrieme },
    { label: '3ème', value: GradeLevel.Troisieme },
    { label: 'Seconde', value: GradeLevel.Seconde },
    { label: 'Première', value: GradeLevel.Premiere },
    { label: 'Terminale', value: GradeLevel.Terminale },
  ];

  constructor() {}

  ngOnInit(): void {
    // 1. On s'abonne une seule fois pour récupérer les infos ET l'ID
    this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.userName = this.formatUserName(user);
        this.userLastName = user.lastName || '';
      }
    });

    this.loadChildren();
    this.loadDashboardData();
  }

  private formatUserName(user: UserDto): string {
      const fullName = `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim();
      if (fullName) {
        return fullName;
      }
      return user.username || '';
    }

  openDetailsModal(child: Child) {
    this.selectedChild = child;
    this.showDetailsModal = true;
  }

  closeDetailsModal() {
    this.showDetailsModal = false;
    this.selectedChild = null;
  }

  loadChildren() {
    this.parentService.getMyChildren().subscribe({
      next: (data) => this.children = data,
      error: (err) => console.error('Impossible de charger les enfants', err)
    });
  }

  loadDashboardData() {
    this.parentService.getUpcomingCourses().subscribe(data => {
      this.courses = data;
      this.nextCourse = this.courses.find(c => 
        (c.status === 'Confirmé' || c.status === 'En attente') && new Date(c.date) > new Date()
      ) || null;
    });

    this.parentService.getLastPayment().subscribe(data => this.lastPayment = data);
    this.parentService.getLastMessage().subscribe(data => this.lastMessage = data);
  }

  get upcomingCourses() {
    return this.courses.filter(c => c.status !== 'Terminé' && c.status !== 'Annulé');
  }

  get historyCourses() {
    return this.courses.filter(c => c.status === 'Terminé' || c.status === 'Annulé');
  }

  toggleTab(tab: 'upcoming' | 'history') {
    this.currentTab = tab;
  }

  openChildModal() {
    this.showChildModal = true;
  }

  closeChildModal() {
    this.showChildModal = false;
    this.newChildName = '';
    this.newChildLevel = null;
    this.newChildBirthDate = '';
    this.newChildPassword = '';
  }

  // Vérification locale de la complexité (reprend la logique de votre image backend)
  private hasPasswordComplexity(password: string): boolean {
    if (password.length < 6) return false;
    const hasUpper = /[A-Z]/.test(password);
    const hasLower = /[a-z]/.test(password);
    const hasDigit = /\d/.test(password);
    return hasUpper && hasLower && hasDigit;
  }

  saveChild() {
    // 1. Validation Champs
    if (!this.newChildName || this.newChildLevel === null || !this.newChildPassword) {
      this.toastService.warning('Veuillez remplir les champs obligatoires.');
      return;
    }

    // 2. Validation Mot de passe
    if (!this.hasPasswordComplexity(this.newChildPassword)) {
      this.toastService.warning('Le mot de passe doit contenir : 6 caractères min, 1 majuscule, 1 minuscule, 1 chiffre.');
      return;
    }

    this.isLoading = true;

    // 4. Construction de la requête avec l'ID valide (String UUID)
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
        this.toastService.success('Compte enfant créé avec succès !');
        this.closeChildModal();
        this.loadChildren();
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Erreur inscription:', err);
        // Si le backend renvoie une erreur, on essaie de l'afficher
        const message = err.error?.error || 'Erreur lors de la création.';
        this.toastService.error(message);
      }
    });
  }

  deleteChild(id: string) {
      if (confirm('Voulez-vous vraiment supprimer ce profil enfant ? Cette action est définitive.')) {
        
        // Appel au service pour la suppression côté serveur
        this.parentService.deleteChild(id).subscribe({
          next: () => {
            // Succès : On retire l'enfant de la liste locale pour éviter de recharger toute la page
            this.children = this.children.filter(c => c.id !== id);
            this.toastService.success('Le profil enfant a été supprimé.');
          },
          error: (err) => {
            console.error('Erreur lors de la suppression :', err);
            // Gestion des erreurs (ex: 404 introuvable, 403 interdit)
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
}
