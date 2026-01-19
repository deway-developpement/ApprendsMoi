import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TextInputComponent } from '../../components/shared/TextInput/text-input.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { HeaderComponent } from '../../components/Header/header.component';

@Component({
  selector: 'app-inscription',
  standalone: true,
  imports: [CommonModule, FormsModule, TextInputComponent, ButtonComponent, HeaderComponent],
  templateUrl: './inscription.component.html',
  styleUrls: ['./inscription.component.scss']
})
export class InscriptionComponent {
  name = '';
  email = '';
  password = '';
  confirmPassword = '';

  onRegister() {
    if (this.password !== this.confirmPassword) {
      alert('Les mots de passe ne correspondent pas');
      return;
    }
    
    console.log('Inscription demandée:', {
      name: this.name,
      email: this.email,
      password: this.password
    });
    // Logique d'appel API ici
  }
}