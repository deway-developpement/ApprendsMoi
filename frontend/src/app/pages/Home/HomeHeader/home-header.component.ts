import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeaderComponent } from '../../../components/Header/header.component';
import { SearchBarComponent } from './SearchBar/search-bar.component';
import { HeroIntroComponent } from './HeroIntro/hero-intro.component';
import { HeroIntroRightComponent } from './HeroIntroRight/hero-intro-right.component';

@Component({
  selector: 'app-home-header',
  standalone: true,
  imports: [CommonModule, HeaderComponent, SearchBarComponent, HeroIntroComponent, HeroIntroRightComponent],
  templateUrl: './home-header.component.html',
  styleUrls: ['./home-header.component.scss']
})
export class HomeHeaderComponent {}
