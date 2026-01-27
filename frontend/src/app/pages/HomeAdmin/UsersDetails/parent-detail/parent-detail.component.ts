import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserDto } from '../../../../services/auth.service';

@Component({
  selector: 'app-parent-detail',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="p-4 bg-white rounded shadow">
      <h2 class="text-xl font-bold text-green-600">Fiche Parent</h2>
      <p>Nom: {{ user.firstName }} {{ user.lastName }}</p>
      </div>
  `
})
export class ParentDetailComponent {
  @Input({ required: true }) user!: UserDto;
}