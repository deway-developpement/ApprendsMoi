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

  // Form Fields
  email = '';
  password = '';
  confirmPassword = '';
  
  // Extra fields required by updated AuthService/Swagger (FirstName, LastName, PhoneNumber)
  firstName = '';
  lastName = '';
  phoneNumber = '';

  // Profile Management
  selectedProfile: number = ProfileType.Parent;
  profileOptions: SelectOption[] = [
    { label: 'Je suis un Parent', value: ProfileType.Parent },
    { label: 'Je suis un Professeur', value: ProfileType.Teacher }
  ];

  isLoading = false;

  private readonly emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

  /**
   * [cite_start]Validates password complexity based on C# backend logic[cite: 1]:
   * - Length >= 6
   * - At least 1 Uppercase
   * - At least 1 Lowercase
   * - At least 1 Digit
   */
  

  onRegister() {
    // --- 1. VALIDATION ---

    // Empty fields check (including new name fields)
    if (!this.email || !this.password || !this.confirmPassword || !this.firstName || !this.lastName) {
      this.toastService.warning('Veuillez remplir tous les champs obligatoires (Nom, Prénom, Email, Mot de passe).');
      return;
    }

    // Email format
    if (!this.emailRegex.test(this.email)) {
      this.toastService.warning('Le format de l\'adresse email est invalide.');
      return;
    }

    // Password mismatch
    if (this.password !== this.confirmPassword) {
      this.toastService.warning('Les mots de passe ne correspondent pas.');
      return;
    }

    // Password Complexity Check (New logic from image)
    if (!this.authService.hasPasswordComplexity(this.password)) {
      this.toastService.warning('Le mot de passe doit contenir au moins 6 caractères, une majuscule, une minuscule et un chiffre.');
      return;
    }

    // --- 2. API CALL ---
    
    this.isLoading = true;

    // Mapping to the updated RegisterRequest interface
    const request: RegisterRequest = {
      email: this.email,
      password: this.password,
      firstName: this.firstName,
      lastName: this.lastName,
      phoneNumber: this.phoneNumber, // Optional
      profile: Number(this.selectedProfile)
    };

    this.authService.register(request).subscribe({
      next: () => {
        this.isLoading = false;
        this.toastService.success('Compte créé avec succès ! Connectez-vous.');
        this.router.navigate(['/login']);
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Erreur inscription:', err);

        // Extract error message safely
        const message = err.error?.detail || err.error?.title || 'Erreur lors de l\'inscription. Vérifiez vos données.';
        this.toastService.error(message);
      }
    });
  }
}