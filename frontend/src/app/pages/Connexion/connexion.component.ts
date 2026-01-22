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
  private router = inject(Router);

  // On utilise 'credential' car ça peut être un email OU un username
  credential = ''; 
  password = '';
  
  // Par défaut Parent
  selectedProfile: number = ProfileType.Parent;

  // Assurez-vous que ProfileType.Student existe dans votre enum auth.models.ts
  // Si ce n'est pas le cas, ajoutez Student = 4 dans l'enum
  profileOptions: SelectOption[] = [
    { label: 'Je suis un Élève', value: ProfileType.Student }, 
    { label: 'Je suis un Parent', value: ProfileType.Parent },
    { label: 'Je suis un Professeur', value: ProfileType.Teacher },
    { label: 'Je suis un Administrateur', value: ProfileType.Admin },
  ];

  isLoading = false;

  // Helper pour savoir si c'est un élève (pour l'affichage)
  get isStudent(): boolean {
    return Number(this.selectedProfile) === ProfileType.Student;
  }

  onSubmit() {
    if (!this.credential || !this.password) {
      this.toastService.warning('Veuillez remplir tous les champs.');
      return;
    }

    this.isLoading = true;

    const request: LoginRequest = {
      credential: this.credential,
      password: this.password,
      isStudent: this.isStudent // Le backend a besoin de savoir si c'est un élève
    };

    this.authService.login(request).subscribe({
      next: () => {
        this.isLoading = false;
        this.toastService.success('Connexion réussie !');
        // La redirection est gérée dans le AuthService via redirectUser()
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Erreur login:', err);
        this.toastService.error('Identifiants incorrects ou compte inexistant.');
      }
    });
  }

  onForgotPassword() {
    this.toastService.info('Fonctionnalité de mot de passe oublié à venir.');
  }
}