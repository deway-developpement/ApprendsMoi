import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeaderComponent } from '../../components/Header/header.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { SmallIconComponent } from '../../components/shared/SmallIcon/small-icon.component';
import { CoursesScheduleComponent } from '../../components/shared/CoursesSchedule/courses-schedule.component';

// Interfaces (Adapted for Teacher context)
interface Course {
  id: number;
  date: Date;
  tutorName: string; // In this case, it's "Me" but kept for compatibility
  childName: string; // The student's name
  subject: string;
  mode: 'Domicile' | 'Visio';
  status: 'Confirmé' | 'En attente' | 'Annulé' | 'Terminé';
  price: number; // Important for revenue calculation
}

interface BookingRequest {
  id: number;
  parentName: string;
  subject: string;
  date: Date;
}

@Component({
  selector: 'app-home-teacher',
  templateUrl: './home-teacher.component.html',
  styleUrls: ['./home-teacher.component.scss'], // You might want to share the SCSS with student or create a shared dashboard.scss
  standalone: true,
  imports: [
    CommonModule,
    HeaderComponent,
    ButtonComponent,
    SmallIconComponent,
    CoursesScheduleComponent
  ]
})
export class HomeTeacherComponent implements OnInit {
  teacherName = 'Marc';
  
  // Data
  nextCourse: Course | null = null;
  courses: Course[] = [];
  pendingRequests: BookingRequest[] = [];
  
  // KPI Data
  currentMonthRevenue: number = 0;
  pendingRevenue: number = 0;

  constructor() {}

  ngOnInit(): void {
    // 1. Load Courses (Mock Data)
    this.courses = [
      { id: 201, date: new Date('2023-11-15T14:00:00'), tutorName: 'Moi', subject: 'Maths', childName: 'Léo D.', mode: 'Domicile', status: 'Confirmé', price: 35 },
      { id: 202, date: new Date('2023-11-16T10:00:00'), tutorName: 'Moi', subject: 'Maths', childName: 'Sarah M.', mode: 'Visio', status: 'Confirmé', price: 30 },
      { id: 203, date: new Date('2023-11-14T09:00:00'), tutorName: 'Moi', subject: 'Physique', childName: 'Tom P.', mode: 'Domicile', status: 'Terminé', price: 35 },
      { id: 204, date: new Date('2023-11-20T17:00:00'), tutorName: 'Moi', subject: 'Maths', childName: 'Léo D.', mode: 'Domicile', status: 'Confirmé', price: 35 },
    ];

    // 2. Find Next Course
    this.nextCourse = this.courses.find(c => 
      c.status === 'Confirmé' && new Date(c.date) > new Date()
    ) || null;

    // 3. Mock Pending Requests (Section 5.2 - Réservations en attente)
    this.pendingRequests = [
      { id: 1, parentName: 'Mme. Dupont', subject: 'Maths (Terminale)', date: new Date('2023-11-18T18:00:00') }
    ];

    // 4. Calculate Revenue (Simple Mock logic)
    this.calculateRevenue();
  }

  private calculateRevenue(): void {
    // Logic: Sum of 'Terminé' courses for current month
    this.currentMonthRevenue = this.courses
      .filter(c => c.status === 'Terminé') // In a real app, verify month matches
      .reduce((acc, curr) => acc + curr.price, 0);

    // Logic: Sum of 'Confirmé' courses (future revenue)
    this.pendingRevenue = this.courses
      .filter(c => c.status === 'Confirmé')
      .reduce((acc, curr) => acc + curr.price, 0);
  }
}