import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service'; // Adjust path
import { ButtonComponent } from '../shared/Button/button.component'; // Adjust path
import { take } from 'rxjs/operators'; // Import take

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, ButtonComponent],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

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

  onNavigate(fragment: string) {
    // 1. Close the menu first (UI interaction)
    this.closeMenu();

    // 2. Check user state once and navigate
    this.user$.pipe(take(1)).subscribe(user => {
      // Logic: If user exists go to root '/', else go to '/home'
      const targetRoute = user ? ['/home'] : ['/'];

      this.router.navigate(targetRoute, { 
        fragment: fragment // Pass the fragment (anchor) here
      });
    });
  }

  onLogout() {
    this.authService.logout();
    this.closeMenu();
  }
}