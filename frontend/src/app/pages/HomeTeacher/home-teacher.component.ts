import { Component } from '@angular/core';
import { HeaderComponent } from '../../components/Header/header.component';

@Component({
    selector: 'app-home-teacher',
    templateUrl: './home-teacher.component.html',
    styleUrls: ['./home-teacher.component.scss'],
    standalone: true,
    imports: [HeaderComponent]
})
export class HomeTeacherComponent {

}