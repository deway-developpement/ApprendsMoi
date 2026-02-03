import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface ModalConfig {
  id: number;
  type: 'confirm' | 'prompt';
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  inputPlaceholder?: string;
  resolve?: (value: boolean | string | null) => void;
}

@Injectable({ providedIn: 'root' })
export class ModalService {
  private modalSubject = new BehaviorSubject<ModalConfig | null>(null);
  modal$ = this.modalSubject.asObservable();
  
  private counter = 0;

  confirm(message: string, title: string = 'Confirmation'): Promise<boolean> {
    return new Promise((resolve) => {
      const id = this.counter++;
      const modal: ModalConfig = {
        id,
        type: 'confirm',
        title,
        message,
        confirmText: 'Confirmer',
        cancelText: 'Annuler',
        resolve: (value) => resolve(value as boolean)
      };
      this.modalSubject.next(modal);
    });
  }

  prompt(message: string, title: string = 'Saisie requise', placeholder: string = ''): Promise<string | null> {
    return new Promise((resolve) => {
      const id = this.counter++;
      const modal: ModalConfig = {
        id,
        type: 'prompt',
        title,
        message,
        confirmText: 'Valider',
        cancelText: 'Annuler',
        inputPlaceholder: placeholder,
        resolve: (value) => resolve(value as string | null)
      };
      this.modalSubject.next(modal);
    });
  }

  close(result: boolean | string | null = false) {
    const currentModal = this.modalSubject.value;
    if (currentModal?.resolve) {
      currentModal.resolve(result);
    }
    this.modalSubject.next(null);
  }
}
