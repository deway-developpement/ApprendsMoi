import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '../../components/Header/header.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { IconComponent } from '../../components/shared/Icon/icon.component';
import { SmallIconComponent } from '../../components/shared/SmallIcon/small-icon.component';
import { IconButtonComponent } from '../../components/shared/IconButton/icon-button.component';
import { TextInputComponent } from '../../components/shared/TextInput/text-input.component';
import { SelectComponent, SelectOption } from '../../components/shared/Select/select.component';
import { CoursesScheduleComponent } from '../../components/shared/CoursesSchedule/courses-schedule.component';

// Interfaces
interface Child {
  id: number;
  firstName: string;
  lastName: string;
  level: string;
  subjects: string[];
  avatarColor: string;
}

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
  userName = 'Sophie';
  currentTab: 'upcoming' | 'history' = 'upcoming';
  showChildModal = false;

  // Data
  nextCourse: Course | null = null;
  lastMessage: Message | null = null;
  lastPayment: Payment | null = null;
  
  children: Child[] = [
    { id: 1, firstName: 'Léa', lastName: 'Dubois', level: '3ème', subjects: ['Maths', 'Physique'], avatarColor: '#fbbf24' }, // Premium Goldish
    { id: 2, firstName: 'Thomas', lastName: 'Dubois', level: 'CM2', subjects: ['Français'], avatarColor: '#4EE381' } // Green
  ];

  courses: Course[] = [
    { id: 101, date: new Date('2023-11-15T14:00:00'), tutorName: 'Julie B.', subject: 'Maths', childName: 'Léa', mode: 'Domicile', status: 'Confirmé', price: 35 },
    { id: 102, date: new Date('2023-11-18T10:00:00'), tutorName: 'Marc D.', subject: 'Français', childName: 'Thomas', mode: 'Visio', status: 'En attente', price: 25 },
    { id: 103, date: new Date('2023-11-01T16:00:00'), tutorName: 'Julie B.', subject: 'Physique', childName: 'Léa', mode: 'Domicile', status: 'Terminé', price: 35 },
  ];

  levelOptions: SelectOption[] = [
    { label: 'Primaire', value: 'Primaire' },
    { label: 'Collège', value: 'Collège' },
    { label: 'Lycée', value: 'Lycée' }
  ];

  // Form Data
  newChildName = '';
  newChildLevel: string | number | null = null;
  newChildPassword = ''; // Added password field

  constructor() {}

  ngOnInit(): void {
    this.nextCourse = this.courses.find(c => c.status === 'Confirmé' || c.status === 'En attente') || null;
    
    this.lastMessage = {
      sender: 'Julie B.',
      preview: 'Bonjour, est-ce qu\'on peut décaler le cours de...',
      date: new Date()
    };

    this.lastPayment = {
      amount: 35,
      status: 'Payé',
      date: new Date('2023-11-02')
    };
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
    this.newChildPassword = ''; // Reset password
  }

  saveChild() {
    // Validate that we have name, level AND password
    if (this.newChildName && this.newChildLevel && this.newChildPassword) {
      console.log('Creating account for child with password:', this.newChildPassword);
      
      this.children.push({
        id: Date.now(),
        firstName: this.newChildName,
        lastName: 'Dubois',
        level: this.newChildLevel.toString(),
        subjects: [],
        avatarColor: '#f97316' // Secondary Color
      });
      this.closeChildModal();
    }
  }

  deleteChild(id: number) {
    this.children = this.children.filter(c => c.id !== id);
  }
}