import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TextInputComponent } from '../../components/shared/TextInput/text-input.component'; // Adjust path
import { ButtonComponent } from '../../components/shared/Button/button.component'; // Adjust path
import { HeaderComponent } from '../../components/Header/header.component';

@Component({
  selector: 'app-connexion',
  standalone: true,
  imports: [CommonModule, FormsModule, TextInputComponent, ButtonComponent, HeaderComponent],
  templateUrl: './connexion.component.html',
  styleUrls: ['./connexion.component.scss']
})
export class ConnexionComponent {
  email = '';
  password = '';

  // Placeholder for future logic
  onSubmit() {
    console.log('Login attempt:', { email: this.email, password: this.password });
  }

  onForgotPassword() {
    console.log('Redirect to forgot password');
  }
}