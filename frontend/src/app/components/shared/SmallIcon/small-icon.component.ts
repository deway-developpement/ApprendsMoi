import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-small-icon',
  standalone: true,
  templateUrl: './small-icon.component.html',
  styleUrls: ['./small-icon.component.scss'],
  imports: [CommonModule]
})
export class SmallIconComponent {
  @Input() icon!: string;
  @Input() size = 24; // always small
}
