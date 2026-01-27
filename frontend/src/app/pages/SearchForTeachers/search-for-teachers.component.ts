import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { HeaderComponent } from '../../components/Header/header.component';
import { SearchBarComponent } from '../Home/HomeHeader/SearchBar/search-bar.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { SelectComponent, SelectOption } from '../../components/shared/Select/select.component';
import { ToastService } from '../../services/toast.service';
import { environment } from '../../environments/environment';

interface SearchCriteria {
  subject: string | number | null;
  level: string | number | null;
  location: string;
}

interface TeacherDto {
  id: string;
  firstName: string;
  lastName: string;
  profilePicture?: string | null;
  bio?: string | null;
  verificationStatus: number;
  isPremium: boolean;
  city?: string | null;
  travelRadiusKm?: number | null;
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
export class SearchForTeachersComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly toastService = inject(ToastService);
  private readonly apiUrl = `${environment.apiUrl}/api/Users/teachers`;

  teachers: TeacherCard[] = [];
  visibleTeachers: TeacherCard[] = [];
  isLoading = false;

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

  ngOnInit(): void {
    this.fetchTeachers();
  }

  applySearch(criteria: SearchCriteria) {
    this.searchCriteria = { ...criteria };
    this.fetchTeachers();
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
    this.formatFilter = this.formatFilter === filter ? 'all' : filter;
    this.updateResults();
  }

  clearFilters() {
    this.searchCriteria = { subject: null, level: null, location: '' };
    this.premiumOnly = false;
    this.verifiedOnly = false;
    this.formatFilter = 'all';
    this.sortValue = 'relevance';
    this.fetchTeachers();
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

  private async fetchTeachers(): Promise<void> {
    this.isLoading = true;
    const city = this.searchCriteria.location?.trim();
    const url = city ? `${this.apiUrl}?city=${encodeURIComponent(city)}` : this.apiUrl;

    try {
      const data = await firstValueFrom(this.http.get<TeacherDto[]>(url));
      this.teachers = (data ?? []).map((teacher) => this.mapTeacherCard(teacher));
      this.updateResults();
    } catch (err) {
      this.toastService.error(this.getErrorMessage(err, 'Unable to load teachers.'));
      this.teachers = [];
      this.visibleTeachers = [];
    } finally {
      this.isLoading = false;
    }
  }

  private updateResults() {
    let result = [...this.teachers];

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

  private mapTeacherCard(teacher: TeacherDto): TeacherCard {
    const fullName = `${teacher.firstName} ${teacher.lastName}`.trim();
    const city = teacher.city || 'Ville inconnue';
    const travelRadius = teacher.travelRadiusKm ?? 0;
    const format = travelRadius >= 10 ? 'Hybride' : travelRadius > 0 ? 'Domicile' : 'Visio';

    return {
      id: teacher.id,
      name: fullName || 'Professeur',
      city,
      format,
      pricePerHour: 30,
      rating: 4.8,
      reviews: 20,
      bio: teacher.bio || 'Profil en cours de mise a jour.',
      subjects: ['Toutes matieres'],
      subjectSlugs: ['all'],
      levels: ['Tous niveaux'],
      levelSlugs: ['all'],
      isPremium: teacher.isPremium,
      isVerified: teacher.verificationStatus === 1,
      isTop: teacher.isPremium,
      avatarColor: this.pickAvatarColor(teacher.id)
    };
  }

  private pickAvatarColor(seed: string): string {
    const colors = ['#1a365d', '#f97316', '#4EE381', '#3b82f6', '#8b5cf6'];
    let hash = 0;
    for (let i = 0; i < seed.length; i++) {
      hash = seed.charCodeAt(i) + ((hash << 5) - hash);
    }
    const index = Math.abs(hash % colors.length);
    return colors[index];
  }

  private getErrorMessage(err: unknown, fallback: string): string {
    if (err instanceof HttpErrorResponse) {
      if (typeof err.error === 'string') return err.error;
      if (err.error?.error) return err.error.error;
      return err.message || fallback;
    }

    if (err instanceof Error) return err.message;
    return fallback;
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
