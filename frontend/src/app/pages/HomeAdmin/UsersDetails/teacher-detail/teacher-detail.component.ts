import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

// Services
import { UserDto } from '../../../../services/auth.service';

// UI Components
import { ButtonComponent } from '../../../../components/shared/Button/button.component';
import { SmallIconComponent } from '../../../../components/shared/SmallIcon/small-icon.component';
import { TeacherReviewsComponent } from '../../../../components/shared/TeacherReviews/teacher-reviews.component';

@Component({
  selector: 'app-teacher-detail',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule,
    ButtonComponent,
    SmallIconComponent,
    TeacherReviewsComponent
  ],
  templateUrl: './teacher-detail.component.html',
  styleUrls: ['./teacher-detail.component.scss']
})
export class TeacherDetailComponent implements OnInit {
  @Input({ required: true }) user!: UserDto;

  // RAW DATA FOR VISUALIZATION (Admin view of this teacher)
  teacherStats = {
    totalRevenueGenerated: 3450.00, // Revenue brought to platform
    commissionGenerated: 517.50, // Platform cut
    rating: 4.8,
    reviewCount: 24,
    studentCount: 12,
    completionRate: 98
  };

  // Documents waiting for admin validation
  documents = [
    { name: "Carte d'identité", status: 'valid', date: '12/01/2023' },
    { name: "Diplôme Master 2", status: 'pending', date: '24/10/2023' },
    { name: "Casier Judiciaire", status: 'valid', date: '12/01/2023' }
  ];

  // Recent activity log specific to this teacher
  recentActivity = [
    { date: 'Hier, 14:00', action: 'Cours terminé', detail: 'Maths avec Lucas (1h)' },
    { date: '23 Oct', action: 'Nouveau cours programmé', detail: 'Anglais avec Sarah' },
    { date: '20 Oct', action: 'Paiement reçu', detail: 'Virement de 120€' }
  ];

  ngOnInit() {
    // In a real app, you would fetch these details using this.user.id
  }

  validateDocument(docName: string) {
    alert(`Validation du document : ${docName}`);
  }

  suspendAccount() {
    if(confirm('Voulez-vous vraiment suspendre ce professeur ?')) {
      alert('Compte suspendu');
    }
  }
}