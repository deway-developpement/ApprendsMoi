import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeaderComponent } from '../../components/Header/header.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { SmallIconComponent } from '../../components/shared/SmallIcon/small-icon.component';
import { IconComponent } from '../../components/shared/Icon/icon.component';

// Interfaces pour les données du dashboard
interface Kpi {
  label: string;
  value: string | number;
  trend?: string; // ex: "+12%"
  icon: string;
  colorClass: 'blue' | 'green' | 'orange' | 'purple';
}

interface AlertItem {
  id: number;
  type: 'document' | 'litige' | 'review';
  title: string;
  subtitle: string;
  date: Date;
  priority: 'high' | 'medium';
}

interface RecentActivity {
  id: number;
  description: string;
  time: string; // ex: "Il y a 2h"
  icon: string;
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
  adminName = 'Alexandre';

  currentPage = 1;
  itemsPerPage = 3;

  kpis: Kpi[] = [
    { label: 'Utilisateurs Actifs', value: '1,240', trend: '+5%', icon: '/assets/icons/people.svg', colorClass: 'blue' },
    { label: 'Revenus du mois', value: '4,520 €', trend: '+12%', icon: '/assets/icons/account-balance.svg', colorClass: 'green' },
    { label: 'Cours ce mois', value: '342', trend: '-2%', icon: '/assets/icons/book-fill.svg', colorClass: 'purple' },
    { label: 'À traiter', value: '8', icon: '/assets/icons/notification.svg', colorClass: 'orange' } // Documents ou litiges
  ];

alerts: AlertItem[] = [
    { id: 1, type: 'document', title: 'Validation Identité', subtitle: 'Prof. Jean Dupont - Carte d\'identité', date: new Date(), priority: 'medium' },
    { id: 2, type: 'litige', title: 'Litige Cours #4092', subtitle: 'Parent: Marie L. vs Prof: Marc D.', date: new Date('2023-11-10T09:00:00'), priority: 'high' },
    { id: 3, type: 'document', title: 'Diplôme à vérifier', subtitle: 'Prof. Sarah Cohen - Master Maths', date: new Date(), priority: 'medium' },
    { id: 4, type: 'review', title: 'Avis signalé', subtitle: 'Commentaire inapproprié sur Prof. X', date: new Date('2023-11-12T14:30:00'), priority: 'high' },
    { id: 5, type: 'document', title: 'Justificatif Domicile', subtitle: 'Prof. Lucas M.', date: new Date('2023-11-13T10:00:00'), priority: 'medium' },
    { id: 6, type: 'litige', title: 'Litige Paiement', subtitle: 'Remboursement non reçu - Famille B.', date: new Date('2023-11-14T08:00:00'), priority: 'high' },
    { id: 7, type: 'document', title: 'Casier Judiciaire', subtitle: 'Prof. Emma W.', date: new Date('2023-11-15T11:20:00'), priority: 'medium' }
  ];

  activities: RecentActivity[] = [
    { id: 101, description: 'Nouvelle inscription : Professeur Lucas M.', time: 'Il y a 10 min', icon: 'person_add' },
    { id: 102, description: 'Réservation confirmée : Cours de Maths (Léa)', time: 'Il y a 35 min', icon: 'check_circle' },
    { id: 103, description: 'Nouveau paiement reçu : 45.00 €', time: 'Il y a 1h', icon: 'payments' },
    { id: 104, description: 'Nouvel avis posté par Mme. Dubois (5/5)', time: 'Il y a 2h', icon: 'star' }
  ];

  constructor() {}

  ngOnInit(): void {}

  // Helpers pour l'affichage
  getPriorityLabel(priority: string): string {
    return priority === 'high' ? 'Urgent' : 'En attente';
  }

  get paginatedAlerts() {
    const startIndex = (this.currentPage - 1) * this.itemsPerPage;
    return this.alerts.slice(startIndex, startIndex + this.itemsPerPage);
  }

  get totalPages() {
    return Math.ceil(this.alerts.length / this.itemsPerPage);
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }
}