import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { HeaderComponent } from '../../components/Header/header.component';
import { SearchBarComponent } from '../Home/HomeHeader/SearchBar/search-bar.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { SelectComponent, SelectOption } from '../../components/shared/Select/select.component';

interface SearchCriteria {
  subject: string | number | null;
  level: string | number | null;
  location: string;
}

interface TeacherCard {
  id: string;
  name: string;
  city: string;
  format: 'Domicile' | 'Visio' | 'Hybride';
  pricePerHour: number;
  rating: number;
  reviews: number;
  bio: string;
  subjects: string[];
  subjectSlugs: string[];
  levels: string[];
  levelSlugs: string[];
  isPremium: boolean;
  isVerified: boolean;
  isTop: boolean;
  avatarColor: string;
}

@Component({
  selector: 'app-search-for-teachers',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    HeaderComponent,
    SearchBarComponent,
    ButtonComponent,
    SelectComponent
  ],
  templateUrl: './search-for-teachers.component.html',
  styleUrls: ['./search-for-teachers.component.scss']
})
export class SearchForTeachersComponent {
  teachers: TeacherCard[] = [
    {
      id: '019bfc6d-893d-782a-87db-da528377e1b6',
      name: 'Camille Robert',
      city: 'Paris',
      format: 'Hybride',
      pricePerHour: 32,
      rating: 4.9,
      reviews: 56,
      bio: 'Specialiste en maths et physique. Methode claire et progressive.',
      subjects: ['Mathematiques', 'Physique-Chimie'],
      subjectSlugs: ['maths', 'physique-chimie'],
      levels: ['Troisieme', 'Seconde', 'Premiere'],
      levelSlugs: ['troisieme', 'seconde', 'premiere'],
      isPremium: true,
      isVerified: true,
      isTop: false,
      avatarColor: '#1a365d'
    },
    {
      id: '019bfc6d-8a81-792f-a9e9-6129d53afd7f',
      name: 'Noah Petit',
      city: 'Lyon',
      format: 'Visio',
      pricePerHour: 26,
      rating: 4.7,
      reviews: 41,
      bio: 'Cours dynamiques en anglais, preparation aux examens.',
      subjects: ['Anglais'],
      subjectSlugs: ['anglais'],
      levels: ['Quatrieme', 'Troisieme', 'Seconde'],
      levelSlugs: ['quatrieme', 'troisieme', 'seconde'],
      isPremium: false,
      isVerified: true,
      isTop: false,
      avatarColor: '#f97316'
    },
    {
      id: '019bfc70-9e81-7d6e-aad5-77797653a390',
      name: 'Lea Benali',
      city: 'Bordeaux',
      format: 'Domicile',
      pricePerHour: 30,
      rating: 5.0,
      reviews: 22,
      bio: 'Francais et histoire-geo avec une approche creative.',
      subjects: ['Francais', 'Histoire-Geographie'],
      subjectSlugs: ['francais', 'histoire-geo'],
      levels: ['Sixieme', 'Cinquieme', 'Quatrieme'],
      levelSlugs: ['sixieme', 'cinquieme', 'quatrieme'],
      isPremium: true,
      isVerified: true,
      isTop: true,
      avatarColor: '#4EE381'
    }
  ];

  visibleTeachers: TeacherCard[] = [...this.teachers];

  searchCriteria: SearchCriteria = {
    subject: null,
    level: null,
    location: ''
  };

  premiumOnly = false;
  verifiedOnly = false;
  formatFilter: 'all' | 'visio' | 'domicile' | 'hybride' = 'all';

  sortValue: string | number = 'relevance';
  sortOptions: SelectOption[] = [
    { label: 'Pertinence', value: 'relevance' },
    { label: 'Note la plus haute', value: 'rating' },
    { label: 'Prix croissant', value: 'price_asc' },
    { label: 'Prix decroissant', value: 'price_desc' }
  ];

  applySearch(criteria: SearchCriteria) {
    this.searchCriteria = { ...criteria };
    this.updateResults();
  }

  togglePremium() {
    this.premiumOnly = !this.premiumOnly;
    this.updateResults();
  }

  toggleVerified() {
    this.verifiedOnly = !this.verifiedOnly;
    this.updateResults();
  }

  setFormat(filter: 'all' | 'visio' | 'domicile' | 'hybride') {
    this.formatFilter = filter;
    this.updateResults();
  }

  clearFilters() {
    this.searchCriteria = { subject: null, level: null, location: '' };
    this.premiumOnly = false;
    this.verifiedOnly = false;
    this.formatFilter = 'all';
    this.sortValue = 'relevance';
    this.visibleTeachers = [...this.teachers];
  }

  get hasActiveFilters(): boolean {
    return !!this.searchCriteria.subject
      || !!this.searchCriteria.level
      || !!this.searchCriteria.location
      || this.premiumOnly
      || this.verifiedOnly
      || this.formatFilter !== 'all'
      || this.sortValue !== 'relevance';
  }

  get activeFilters(): string[] {
    const filters: string[] = [];
    if (this.searchCriteria.subject) filters.push('Matiere');
    if (this.searchCriteria.level) filters.push('Niveau');
    if (this.searchCriteria.location) filters.push(`Ville: ${this.searchCriteria.location}`);
    if (this.premiumOnly) filters.push('Premium');
    if (this.verifiedOnly) filters.push('Verifie');
    if (this.formatFilter !== 'all') filters.push(`Format: ${this.formatLabel(this.formatFilter)}`);
    return filters;
  }

  onSortChange() {
    this.updateResults();
  }

  private updateResults() {
    let result = [...this.teachers];

    if (this.searchCriteria.subject) {
      const subject = String(this.searchCriteria.subject);
      result = result.filter(teacher => teacher.subjectSlugs.includes(subject));
    }

    if (this.searchCriteria.level) {
      const level = String(this.searchCriteria.level);
      result = result.filter(teacher => teacher.levelSlugs.includes(level));
    }

    if (this.searchCriteria.location) {
      const query = this.searchCriteria.location.toLowerCase();
      result = result.filter(teacher => teacher.city.toLowerCase().includes(query));
    }

    if (this.premiumOnly) {
      result = result.filter(teacher => teacher.isPremium);
    }

    if (this.verifiedOnly) {
      result = result.filter(teacher => teacher.isVerified);
    }

    if (this.formatFilter !== 'all') {
      result = result.filter(teacher => teacher.format.toLowerCase() === this.formatFilter);
    }

    result = this.sortTeachers(result);
    this.visibleTeachers = result;
  }

  private sortTeachers(teachers: TeacherCard[]): TeacherCard[] {
    const sorted = [...teachers];

    switch (this.sortValue) {
      case 'rating':
        sorted.sort((a, b) => b.rating - a.rating);
        break;
      case 'price_asc':
        sorted.sort((a, b) => a.pricePerHour - b.pricePerHour);
        break;
      case 'price_desc':
        sorted.sort((a, b) => b.pricePerHour - a.pricePerHour);
        break;
      default:
        break;
    }

    return sorted;
  }

  formatLabel(value: 'all' | 'visio' | 'domicile' | 'hybride'): string {
    switch (value) {
      case 'visio':
        return 'Visio';
      case 'domicile':
        return 'Domicile';
      case 'hybride':
        return 'Hybride';
      default:
        return 'Tous';
    }
  }
}
