import { Component, OnInit } from '@angular/core';
import { ROUTES } from '../../../constants';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss'],
})
export class NavbarComponent implements OnInit {
  routes = ROUTES;

  constructor() {}

  ngOnInit(): void {}
}
