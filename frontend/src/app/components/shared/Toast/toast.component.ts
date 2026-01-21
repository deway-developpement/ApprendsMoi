import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, Toast } from '../../../services/toast.service'; // Ajustez le chemin
import { SmallIconComponent } from '../SmallIcon/small-icon.component'; // Ajustez le chemin
import { trigger, state, style, transition, animate } from '@angular/animations';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule, SmallIconComponent],
  templateUrl: './toast.component.html',
  styleUrls: ['./toast.component.scss'],
  animations: [
    trigger('toastAnimation', [
      state('void', style({ transform: 'translateX(100%)', opacity: 0 })),
      transition(':enter', [
        animate('300ms ease-out', style({ transform: 'translateX(0)', opacity: 1 }))
      ]),
      transition(':leave', [
        animate('200ms ease-in', style({ transform: 'translateX(100%)', opacity: 0 }))
      ])
    ])
  ]
})
export class ToastComponent {
  toastService = inject(ToastService);
  toasts$ = this.toastService.toasts$;

  getIcon(type: string): string {
    switch(type) {
      case 'success': return 'assets/icons/check-circle.svg'; // Assurez-vous d'avoir ces icônes
      case 'error': return 'assets/icons/alert-circle.svg';
      case 'warning': return 'assets/icons/alert-triangle.svg';
      default: return 'assets/icons/info.svg';
    }
  }

  close(id: number) {
    this.toastService.remove(id);
  }
}