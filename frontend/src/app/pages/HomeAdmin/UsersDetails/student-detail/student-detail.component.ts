import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

// Services
import { UserDto } from '../../../../services/auth.service';

// UI Components
import { ButtonComponent } from '../../../../components/shared/Button/button.component';
import { IconComponent } from '../../../../components/shared/Icon/icon.component';
import { SmallIconComponent } from '../../../../components/shared/SmallIcon/small-icon.component';

@Component({
  selector: 'app-student-detail',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule,
    ButtonComponent,
    IconComponent,
    SmallIconComponent
  ],
  templateUrl: './student-detail.component.html',
  styleUrls: ['./student-detail.component.scss'] // Reusing the same dashboard style
})
export class StudentDetailComponent implements OnInit {
  @Input({ required: true }) user!: UserDto;

  // RAW ADMIN DATA (Mock)
  studentStats = {
    gradeLevel: 'Terminale',
    coursesCompleted: 42,
    hoursConsumed: 68,
    averageAttendance: '100%',
    parentName: 'Marie Curie',
    parentId: 'uuid-parent-123' 
  };

  // Schedule for Admin review
  upcomingCourses = [
    { date: 'Demain, 17:00', subject: 'Mathématiques', tutor: 'Jean Dupont', status: 'confirmed' },
    { date: 'Mercredi, 14:00', subject: 'Physique', tutor: 'Albert E.', status: 'pending' },
    { date: 'Samedi, 10:00', subject: 'Anglais', tutor: 'Sarah C.', status: 'confirmed' }
  ];

  // Recent system logs
  activityLogs = [
    { date: 'Hier, 18:05', action: 'Connexion détectée', detail: 'IP: 192.168.1.1' },
    { date: '20 Oct', action: 'Cours validé', detail: 'Maths (1h)' },
    { date: '15 Oct', action: 'Modification profil', detail: 'Photo de profil mise à jour' }
  ];

  ngOnInit() {
    // Fetch logic would go here
  }

  suspendStudent() {
    if(confirm('Voulez-vous suspendre cet élève ?')) {
      alert('Élève suspendu.');
    }
  }
}