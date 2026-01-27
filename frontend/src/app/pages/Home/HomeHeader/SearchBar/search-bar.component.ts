import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router'; // 1. Import Router

import { SelectComponent, SelectOption } from '../../../../components/shared/Select/select.component';
import { TextInputComponent } from '../../../../components/shared/TextInput/text-input.component';
import { IconButtonComponent } from '../../../../components/shared/IconButton/icon-button.component';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    SelectComponent,
    TextInputComponent,
    IconButtonComponent,
  ],
  templateUrl: './search-bar.component.html',
  styleUrls: ['./search-bar.component.scss'],
})
export class SearchBarComponent {
  subjects: SelectOption[] = [
    { label: 'Mathématiques', value: 'maths' },
    { label: 'Physique-Chimie', value: 'physique-chimie' },
    { label: 'SVT', value: 'svt' },
    { label: 'Français', value: 'francais' },
    { label: 'Histoire-Géographie', value: 'histoire-geo' },
    { label: 'Anglais', value: 'anglais' },
  ];

  levels: SelectOption[] = [
    { label: 'Sixième', value: 'sixieme' },
    { label: 'Cinquième', value: 'cinquieme' },
    { label: 'Quatrième', value: 'quatrieme' },
    { label: 'Troisième', value: 'troisieme' },
    { label: 'Seconde', value: 'seconde' },
    { label: 'Première', value: 'premiere' },
    { label: 'Terminale', value: 'terminale' },
  ];

  selectedSubject: string | number | null = null;
  selectedLevel: string | number | null = null;
  location: string = '';

  @Output() search = new EventEmitter<{
    subject: string | number | null;
    level: string | number | null;
    location: string;
  }>();

  // 2. Inject the Router in the constructor
  constructor(private router: Router) {}

  onSearch() {
    // Optional: Keep this if you still want to notify the parent component
    this.search.emit({
      subject: this.selectedSubject,
      level: this.selectedLevel,
      location: this.location,
    });

    // 3. Use the router to navigate to the register page
    this.router.navigate(['/register']);
  }
}