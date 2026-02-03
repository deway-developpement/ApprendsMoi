import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
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
  imports: [CommonModule, IconButtonComponent, RouterLink],
  templateUrl: './courses-schedule.component.html',
  styleUrls: ['./courses-schedule.component.scss']
})
export class CoursesScheduleComponent {
  @Input() courses: Course[] = [];
  @Input() showPlanningLink = false;
  @Output() action = new EventEmitter<Course>();
  
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

  onActionClick(course: Course) {
    this.action.emit(course);
  }
}
