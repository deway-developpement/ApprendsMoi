import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconButtonComponent } from '../IconButton/icon-button.component'; // Ajustez le chemin selon votre structure

export interface Course {
  id: number;
  date: Date;
  tutorName: string;
  subject: string;
  childName: string;
  mode: 'Domicile' | 'Visio';
  status: 'Confirmé' | 'En attente' | 'Annulé' | 'Terminé';
  price: number;
}

@Component({
  selector: 'app-courses-schedule',
  standalone: true,
  imports: [CommonModule, IconButtonComponent],
  templateUrl: './courses-schedule.component.html',
  styleUrls: ['./courses-schedule.component.scss']
})
export class CoursesScheduleComponent {
  @Input() courses: Course[] = [];
  
  currentTab: 'upcoming' | 'history' = 'upcoming';

  get upcomingCourses() {
    return this.courses.filter(c => c.status !== 'Terminé' && c.status !== 'Annulé');
  }

  get historyCourses() {
    return this.courses.filter(c => c.status === 'Terminé' || c.status === 'Annulé');
  }

  toggleTab(tab: 'upcoming' | 'history') {
    this.currentTab = tab;
  }
}