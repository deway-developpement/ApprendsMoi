import { Component } from '@angular/core';
import { HomeHeaderComponent } from './HomeHeader/home-header.component';

@Component({
  standalone: true,
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
  imports: [HomeHeaderComponent]
})
export class Home {}
