import { Component } from '@angular/core';
import { HeaderComponent } from '../../components/Header/header.component';

@Component({
    selector: 'app-home-parent',
    templateUrl: './home-parent.component.html',
    styleUrls: ['./home-parent.component.css'],
    standalone: true,
    imports: [HeaderComponent]
})
export class HomeParentComponent {
    constructor() {}
}