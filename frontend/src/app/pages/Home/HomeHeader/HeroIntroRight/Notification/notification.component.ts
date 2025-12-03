import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../../../../../components/shared/Icon/icon.component';

@Component({
  selector: 'app-notification',
  templateUrl: './notification.component.html',
  styleUrls: ['./notification.component.scss'],
  standalone: true,
  imports: [CommonModule, IconComponent],
})
export class NotificationComponent {
  @Input() icon!: string;        // chemin de l'icône (svg, png, etc.)
  @Input() title!: string;       // titre de la notification
  @Input() subtitle!: string;    // sous-titre / description
}
