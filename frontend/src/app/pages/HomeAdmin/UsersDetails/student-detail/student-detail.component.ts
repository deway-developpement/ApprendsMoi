import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserDto } from '../../../../services/auth.service';

@Component({
  selector: 'app-student-detail',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="p-4 bg-white rounded shadow">
      <h2 class="text-xl font-bold text-blue-900">Fiche Élève</h2>
      <p>Nom: {{ user.firstName }} {{ user.lastName }}</p>
      </div>
  `
})
export class StudentDetailComponent {
  @Input({ required: true }) user!: UserDto;
}