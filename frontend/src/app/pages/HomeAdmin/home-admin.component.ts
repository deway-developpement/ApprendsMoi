import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { HeaderComponent } from '../../components/Header/header.component';
import { IconComponent } from '../../components/shared/Icon/icon.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { SmallIconComponent } from '../../components/shared/SmallIcon/small-icon.component';
import { environment } from '../../environments/environment';
import { TeacherDocumentDto } from '../Documents/documents.models';

interface Kpi {
  label: string;
  value: string;
  icon: string;
  colorClass: 'blue' | 'green' | 'purple';
}

interface AlertItem {
  id: string;
  type: 'document';
  title: string;
  subtitle: string;
  date: Date;
  priority: 'medium';
}

interface RecentActivity {
  id: number;
  description: string;
  time: string;
  icon: string;
}

interface AdminStatsDto {
  activeUsersThisMonth: number;
  commissionsThisMonth: number;
  completedCoursesThisMonth: number;
}

interface StatsResponseDto {
  userType?: string | null;
  stats?: Record<string, unknown> | null;
}

@Component({
  selector: 'app-home-admin',
  templateUrl: './home-admin.component.html',
  styleUrls: ['./home-admin.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    HeaderComponent,
    ButtonComponent,
    SmallIconComponent,
    IconComponent
  ]
})
export class HomeAdminComponent implements OnInit {
  private readonly apiUrl = environment.apiUrl;
  private readonly numberFormatter = new Intl.NumberFormat('fr-FR');
  private readonly currencyFormatter = new Intl.NumberFormat('fr-FR', {
    style: 'currency',
    currency: 'EUR',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });

  currentPage = 1;
  itemsPerPage = 3;

  kpis: Kpi[] = this.buildKpis({
    activeUsersThisMonth: 0,
    commissionsThisMonth: 0,
    completedCoursesThisMonth: 0
  });

  alerts: AlertItem[] = [];

  activities: RecentActivity[] = [
    { id: 101, description: 'Nouvelle inscription : Professeur Lucas M.', time: 'Il y a 1 jour', icon: 'person_add' },
    { id: 102, description: 'Réservation confirmée : Cours de Maths (Léa)', time: 'Il y a 3 jours', icon: 'check_circle' },
    { id: 103, description: 'Nouveau paiement reçu : 45.00 €', time: 'Il y a 5 jours', icon: 'payments' },
    { id: 104, description: 'Nouvel avis posté par Mme. Dubois (5/5)', time: 'Il y a 1 sem', icon: 'star' }
  ];

  constructor(private router: Router, private http: HttpClient) {}

  async ngOnInit(): Promise<void> {
    await Promise.all([
      this.loadAdminStats(),
      this.loadPendingDocuments()
    ]);
  }

  private async loadAdminStats(): Promise<void> {
    try {
      const response = await firstValueFrom(
        this.http.get<StatsResponseDto>(`${this.apiUrl}/api/stats`)
      );

      const stats = response?.stats;
      if (!stats) {
        return;
      }

      this.kpis = this.buildKpis(this.extractAdminStats(stats));
    } catch (error) {
      console.error('Error loading admin stats:', error);
    }
  }

  private async loadPendingDocuments(): Promise<void> {
    try {
      const pendingDocuments = await firstValueFrom(
        this.http.get<TeacherDocumentDto[]>(`${this.apiUrl}/api/documents/pending`)
      );

      this.alerts = (pendingDocuments ?? []).map(document => this.mapDocumentToAlert(document));
      this.currentPage = 1;
    } catch (error) {
      console.error('Error loading pending documents:', error);
      this.alerts = [];
    }
  }

  private buildKpis(stats: AdminStatsDto): Kpi[] {
    return [
      {
        label: 'Utilisateurs Actifs',
        value: this.numberFormatter.format(stats.activeUsersThisMonth),
        icon: '/assets/icons/people.svg',
        colorClass: 'blue'
      },
      {
        label: 'Revenus du mois',
        value: this.currencyFormatter.format(stats.commissionsThisMonth),
        icon: '/assets/icons/account-balance.svg',
        colorClass: 'green'
      },
      {
        label: 'Cours ce mois',
        value: this.numberFormatter.format(stats.completedCoursesThisMonth),
        icon: '/assets/icons/book-fill.svg',
        colorClass: 'purple'
      }
    ];
  }

  private mapDocumentToAlert(document: TeacherDocumentDto): AlertItem {
    const uploadedAt = new Date(document.uploadedAt);
    const date = Number.isNaN(uploadedAt.getTime()) ? new Date() : uploadedAt;
    const teacherName = this.getTeacherName(document.teacherFirstName, document.teacherLastName);
    const fileName = (document.fileName ?? '').trim() || 'Fichier';

    return {
      id: document.id,
      type: 'document',
      title: `${this.getDocumentTypeLabel(document.documentType)} en attente`,
      subtitle: `${teacherName} - ${fileName}`,
      date,
      priority: 'medium'
    };
  }

  private getTeacherName(firstName?: string, lastName?: string): string {
    const fullName = `${firstName ?? ''} ${lastName ?? ''}`.trim();
    return fullName || 'Professeur inconnu';
  }

  private getDocumentTypeLabel(type: number | string): string {
    if (type === 0 || type === 'ID_PAPER') {
      return "Pièce d'identité";
    }

    if (type === 1 || type === 'DIPLOMA') {
      return 'Diplôme';
    }

    return 'Document';
  }

  private toNumber(value: unknown): number {
    const numericValue = Number(value);
    return Number.isFinite(numericValue) ? numericValue : 0;
  }

  private extractAdminStats(stats: Record<string, unknown>): AdminStatsDto {
    return {
      activeUsersThisMonth: this.toNumber(stats['activeUsersThisMonth'] ?? stats['ActiveUsersThisMonth']),
      commissionsThisMonth: this.toNumber(stats['commissionsThisMonth'] ?? stats['CommissionsThisMonth']),
      completedCoursesThisMonth: this.toNumber(stats['completedCoursesThisMonth'] ?? stats['CompletedCoursesThisMonth'])
    };
  }

  get paginatedAlerts(): AlertItem[] {
    const startIndex = (this.currentPage - 1) * this.itemsPerPage;
    return this.alerts.slice(startIndex, startIndex + this.itemsPerPage);
  }

  get totalPages(): number {
    return Math.ceil(this.alerts.length / this.itemsPerPage);
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }
}
