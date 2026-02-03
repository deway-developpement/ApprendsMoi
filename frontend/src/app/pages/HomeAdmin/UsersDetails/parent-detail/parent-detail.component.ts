import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

// Services
import { UserDto } from '../../../../services/auth.service';
import { ToastService } from '../../../../services/toast.service';

// UI Components
import { ButtonComponent } from '../../../../components/shared/Button/button.component';
import { IconComponent } from '../../../../components/shared/Icon/icon.component';
import { SmallIconComponent } from '../../../../components/shared/SmallIcon/small-icon.component';

@Component({
  selector: 'app-parent-detail',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule,
    ButtonComponent,
    IconComponent,
    SmallIconComponent
  ],
  templateUrl: './parent-detail.component.html',
  styleUrls: ['./parent-detail.component.scss']
})
export class ParentDetailComponent implements OnInit {
  @Input({ required: true }) user!: UserDto;

  // RAW ADMIN DATA (Mock)
  parentStats = {
    totalSpent: 1250.00,
    coursesBooked: 45,
    activeChildren: 2,
    paymentStatus: 'Good Standing', // or 'Late'
    lastLogin: 'Hier, 19:30'
  };

  // Linked Children Accounts
  children = [
    { id: 101, firstName: 'Lucas', level: 'Terminale', avatarColor: '#3b82f6' },
    { id: 102, firstName: 'Emma', level: '3ème', avatarColor: '#8b5cf6' }
  ];

  // Financial History
  transactions = [
    { id: '#INV-2023-001', date: '01 Nov 2023', amount: 120.00, status: 'paid', detail: 'Pack 5h Maths' },
    { id: '#INV-2023-002', date: '15 Oct 2023', amount: 45.00, status: 'paid', detail: 'Cours Anglais (1h)' },
    { id: '#INV-2023-003', date: '01 Oct 2023', amount: 250.00, status: 'paid', detail: 'Pack Rentrée' }
  ];

  ngOnInit() {
    // Fetch logic would go here
  }

  toastService = inject(ToastService);

  suspendParent() {
    if(confirm('Voulez-vous suspendre ce compte parent ? Cela bloquera également les réservations.')) {
      this.toastService.success('Compte parent suspendu.');
    }
  }
}