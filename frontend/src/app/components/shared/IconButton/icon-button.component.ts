import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-icon-button',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './icon-button.component.html',
  styleUrls: ['./icon-button.component.scss'],
})
export class IconButtonComponent {
  @Input() icon: string = ''; // ex: 'assets/icons/search.svg'
  @Input() size: number = 48;
  @Output() pressed = new EventEmitter<void>(); // taille du carré (px)
}
