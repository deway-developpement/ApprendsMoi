import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastComponent } from './components/shared/Toast/toast.component';
import { ModalComponent } from './components/shared/Modal/modal.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastComponent, ModalComponent],
  template: `
    <router-outlet></router-outlet>
    <app-toast-container></app-toast-container>
    <app-modal-container></app-modal-container>
  `,
  styles: []
})
export class AppComponent {
  title = 'frontend';
}
