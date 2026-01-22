import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeaderComponent } from '../../components/Header/header.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { SmallIconComponent } from '../../components/shared/SmallIcon/small-icon.component';
import { CoursesScheduleComponent } from '../../components/shared/CoursesSchedule/courses-schedule.component';

// Interfaces (Simplified for Student context)
interface Course {
  id: number;
  date: Date;
  tutorName: string;
  subject: string;
  childName: string; // Even if it's the student, we keep the prop for compatibility with the shared component
  mode: 'Domicile' | 'Visio';
  status: 'Confirmé' | 'En attente' | 'Annulé' | 'Terminé';
  price: number;
}

interface Message {
  sender: string;
  preview: string;
  date: Date;
}

@Component({
  selector: 'app-home-student',
  templateUrl: './home-student.component.html',
  styleUrls: ['./home-student.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    HeaderComponent,
    ButtonComponent,
    SmallIconComponent,
    CoursesScheduleComponent
  ]
})
export class HomeStudentComponent implements OnInit {
  userName = 'Léa';
  
  // Data
  nextCourse: Course | null = null;
  lastMessage: Message | null = null;

  courses: Course[] = [
    { id: 101, date: new Date('2023-11-15T14:00:00'), tutorName: 'Julie B.', subject: 'Maths', childName: 'Moi', mode: 'Domicile', status: 'Confirmé', price: 0 },
    { id: 103, date: new Date('2023-11-01T16:00:00'), tutorName: 'Julie B.', subject: 'Physique', childName: 'Moi', mode: 'Domicile', status: 'Terminé', price: 0 },
    { id: 104, date: new Date('2023-11-20T17:00:00'), tutorName: 'Marc D.', subject: 'Anglais', childName: 'Moi', mode: 'Visio', status: 'Confirmé', price: 0 },
  ];

  constructor() {}

  ngOnInit(): void {
    // Find next confirmed course
    this.nextCourse = this.courses.find(c => 
      (c.status === 'Confirmé' || c.status === 'En attente') && new Date(c.date) > new Date()
    ) || this.courses[0]; // Fallback for demo
    
    this.lastMessage = {
      sender: 'Julie B.',
      preview: 'N\'oublie pas de faire l\'exercice 3 page 12 pour demain !',
      date: new Date()
    };
  }
}