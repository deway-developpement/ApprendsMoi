import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalService, ModalConfig } from '../../../services/modal.service';
import { ButtonComponent } from '../Button/button.component';
import { trigger, state, style, transition, animate } from '@angular/animations';

@Component({
  selector: 'app-modal-container',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent],
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss'],
  animations: [
    trigger('modalAnimation', [
      state('void', style({ opacity: 0 })),
      transition(':enter', [
        animate('200ms ease-out', style({ opacity: 1 }))
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ opacity: 0 }))
      ])
    ]),
    trigger('dialogAnimation', [
      state('void', style({ transform: 'scale(0.9)', opacity: 0 })),
      transition(':enter', [
        animate('200ms ease-out', style({ transform: 'scale(1)', opacity: 1 }))
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ transform: 'scale(0.9)', opacity: 0 }))
      ])
    ])
  ]
})
export class ModalComponent {
  modalService = inject(ModalService);
  modal$ = this.modalService.modal$;
  
  inputValue = '';

  onConfirm(modal: ModalConfig) {
    if (modal.type === 'prompt') {
      this.modalService.close(this.inputValue);
      this.inputValue = '';
    } else {
      this.modalService.close(true);
    }
  }

  onCancel() {
    this.modalService.close(null);
    this.inputValue = '';
  }

  onBackdropClick() {
    this.onCancel();
  }

  onDialogClick(event: Event) {
    event.stopPropagation();
  }
}
