import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service'; // Adjust path
import { ButtonComponent } from '../shared/Button/button.component'; // Adjust path

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, ButtonComponent],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent {
  private authService = inject(AuthService);

  // Observable of the current user (UserDto | null)
  user$ = this.authService.currentUser$;

  @Input() theme: 'light' | 'dark' = 'dark';
  @Input() simple: 'false' | 'true' = 'false';

  isMenuOpen = false;

  toggleMenu() {
    this.isMenuOpen = !this.isMenuOpen;
  }

  closeMenu() {
    this.isMenuOpen = false;
  }

  onLogout() {
    this.authService.logout();
    this.closeMenu();
  }
}