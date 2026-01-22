import { Component, OnInit } from '@angular/core';
import { HeaderComponent } from '../../components/Header/header.component';

@Component({
    selector: 'app-home-student',
    templateUrl: './home-student.component.html',
    styleUrls: ['./home-student.component.scss'],
    standalone: true,
    imports: [HeaderComponent]
})
export class HomeStudentComponent implements OnInit {

    constructor() { }

    ngOnInit(): void {
    }

}