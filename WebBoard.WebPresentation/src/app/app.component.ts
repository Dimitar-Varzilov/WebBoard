import { Component, OnInit, OnDestroy } from '@angular/core';
import { SignalRService } from './services/signalr.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'web-board.web-presentation';

  constructor(private signalRService: SignalRService) {}

  ngOnInit(): void {
    // Start SignalR connection when app initializes
    this.signalRService.startConnection();
  }

  ngOnDestroy(): void {
    // Stop SignalR connection when app destroys
    this.signalRService.stopConnection();
  }
}
