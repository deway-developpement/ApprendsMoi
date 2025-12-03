import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationComponent } from './Notification/notification.component';
import { SmallIconComponent } from '../../../../components/shared/SmallIcon/small-icon.component';

@Component({
  selector: 'app-hero-intro-right',
  templateUrl: './hero-intro-right.component.html',
  styleUrls: ['./hero-intro-right.component.scss'],
  standalone: true,
  imports: [CommonModule, NotificationComponent, SmallIconComponent],
})
export class HeroIntroRightComponent { }
