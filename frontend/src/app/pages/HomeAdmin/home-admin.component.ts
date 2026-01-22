import { Component, OnInit } from '@angular/core';
import { HeaderComponent } from '../../components/Header/header.component';

@Component({
    selector: 'app-home-admin',
    templateUrl: './home-admin.component.html',
    styleUrls: ['./home-admin.component.scss'],
    standalone: true,
    imports: [HeaderComponent]
})
export class HomeAdminComponent implements OnInit {

    constructor() { }
    ngOnInit(): void {
    }

}