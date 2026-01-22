import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

// Services & Models
import { AuthService, RegisterRequest, ProfileType } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';

// UI Components
import { TextInputComponent } from '../../components/shared/TextInput/text-input.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { HeaderComponent } from '../../components/Header/header.component';
import { SelectComponent, SelectOption } from '../../components/shared/Select/select.component';

@Component({
  selector: 'app-inscription',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    TextInputComponent, 
    ButtonComponent, 
    HeaderComponent,
    SelectComponent
  ],
  templateUrl: './inscription.component.html',
  styleUrls: ['./inscription.component.scss']
})
export class InscriptionComponent {
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private router = inject(Router);

  email = '';
  password = '';
  confirmPassword = '';
  
  // Profile Management (Default to Parent)
  selectedProfile: number = ProfileType.Parent;

  profileOptions: SelectOption[] = [
    { label: 'Je suis un Parent', value: ProfileType.Parent },
    { label: 'Je suis un Professeur', value: ProfileType.Teacher }
  ];

  isLoading = false;

  private readonly emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

  onRegister() {
    // --- 1. WARNING TOASTS (Validation) ---

    // Check for empty fields
    if (!this.email || !this.password || !this.confirmPassword) {
      this.toastService.warning('Veuillez remplir tous les champs obligatoires.');
      return; // STOP SUBMIT
    }

    if (!this.emailRegex.test(this.email)) {
      this.toastService.warning('Le format de l\'adresse email est invalide.');
      return; // STOP SUBMIT
    }

    // Check for password mismatch
    if (this.password !== this.confirmPassword) {
      this.toastService.warning('Les mots de passe ne correspondent pas.');
      return; // STOP SUBMIT
    }

    // Optional: Check password length (Good practice)
    if (this.password.length < 6) {
      this.toastService.warning('Le mot de passe doit contenir au moins 6 caractères.');
      return; // STOP SUBMIT
    }

    // --- 2. API CALL ---
    
    this.isLoading = true;

    const request: RegisterRequest = {
      email: this.email,
      password: this.password,
      profile: Number(this.selectedProfile)
    };

    this.authService.register(request).subscribe({
      next: () => {
        this.isLoading = false;
        
        // --- 3. SUCCESS TOAST ---
        this.toastService.success('Compte créé avec succès ! Connectez-vous.');
        this.router.navigate(['/login']);
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Erreur inscription:', err);

        // --- 4. ERROR TOAST ---
        // Try to get specific message from backend (ProblemDetails), otherwise generic message
        const message = err.error?.detail || err.error?.title || 'Erreur lors de l\'inscription. Vérifiez vos données.';
        this.toastService.error(message);
      }
    });
  }
}