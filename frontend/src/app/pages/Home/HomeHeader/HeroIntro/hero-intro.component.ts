import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonComponent } from '../../../../components/shared/Button/button.component'; // adapte le chemin si besoin

@Component({
  selector: 'app-hero-intro',
  standalone: true,
  imports: [CommonModule, ButtonComponent],
  templateUrl: './hero-intro.component.html',
  styleUrls: ['./hero-intro.component.scss'],
})
export class HeroIntroComponent {
  scrollToSection(sectionId: string): void {
    const element = document.getElementById(sectionId);
    if (element) {
      element.scrollIntoView({ 
        behavior: 'smooth', 
        block: 'start', 
        inline: 'nearest' 
      });
    }
  }
}
