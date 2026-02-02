import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-icon',
  templateUrl: './icon.component.html',
  standalone: true,
  imports: [CommonModule],
  styleUrls: ['./icon.component.scss']
})
export class IconComponent {
  @Input() icon!: string;
  @Input() size = 32;
  @Input() invertColor = false; // New boolean to toggle behavior
}