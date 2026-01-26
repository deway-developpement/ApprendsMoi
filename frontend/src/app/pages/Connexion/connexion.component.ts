import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

// Services & Models
import { AuthService, LoginRequest, ProfileType } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';

// UI Components
import { TextInputComponent } from '../../components/shared/TextInput/text-input.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { HeaderComponent } from '../../components/Header/header.component';
import { SelectComponent, SelectOption } from '../../components/shared/Select/select.component';

@Component({
  selector: 'app-connexion',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    TextInputComponent, 
    ButtonComponent, 
    HeaderComponent, 
    SelectComponent
  ],
  templateUrl: './connexion.component.html',
  styleUrls: ['./connexion.component.scss']
})
export class ConnexionComponent {
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private router = inject(Router); // Utilisé indirectement via AuthService, mais injecté si besoin de redirections spécifiques

  // Champs du formulaire
  credential = ''; // Sera Email ou Username
  password = '';
  
  // Gestion du profil sélectionné (par défaut Parent)
  selectedProfile: number = ProfileType.Parent;

  // Options du menu déroulant
  profileOptions: SelectOption[] = [
    { label: 'Je suis un Élève', value: ProfileType.Student }, 
    { label: 'Je suis un Parent', value: ProfileType.Parent },
    { label: 'Je suis un Professeur', value: ProfileType.Teacher },
    { label: 'Je suis un Administrateur', value: ProfileType.Admin },
  ];

  isLoading = false;

  /**
   * Helper pour déterminer si l'utilisateur se connecte en tant qu'élève.
   * Utilisé pour adapter l'UI (Email vs Username) et la requête API.
   */
  get isStudent(): boolean {
    return Number(this.selectedProfile) === ProfileType.Student;
  }

  onSubmit() {
    // 1. Validation basique
    if (!this.credential || !this.password) {
      this.toastService.warning('Veuillez remplir tous les champs.');
      return;
    }

    this.isLoading = true;

    // 2. Construction de la requête selon le Swagger
    const request: LoginRequest = {
      credential: this.credential,
      password: this.password,
      isStudent: this.isStudent // Crucial pour le backend
    };

    // 3. Appel API
    this.authService.login(request).subscribe({
      next: () => {
        this.isLoading = false;
        this.toastService.success('Connexion réussie !');
        // La redirection est gérée automatiquement dans authService.login() -> tap(redirectUser)
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Erreur login:', err);
        
        // Gestion d'erreur plus fine si possible
        const message = err.error?.detail || 'Identifiants incorrects ou compte inexistant.';
        this.toastService.error(message);
      }
    });
  }

  onForgotPassword() {
    // TODO: Implémenter la navigation vers /forgot-password
    this.router.navigate(['/forgot-password']); 
    // Ou afficher un toast si la page n'existe pas encore :
    // this.toastService.info('Fonctionnalité de mot de passe oublié à venir.');
  }
}