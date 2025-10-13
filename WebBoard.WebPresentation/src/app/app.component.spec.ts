import { TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';

import { AppComponent } from './app.component';
import { SignalRService } from './services';

describe('AppComponent', () => {
  let mockSignalRService: jasmine.SpyObj<SignalRService>;

  beforeEach(async () => {
    mockSignalRService = jasmine.createSpyObj('SignalRService', [
      'startConnection',
      'stopConnection',
    ]);

    await TestBed.configureTestingModule({
      imports: [RouterTestingModule],
      declarations: [AppComponent],
      providers: [{ provide: SignalRService, useValue: mockSignalRService }],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it(`should have as title 'web-board.web-presentation'`, () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app.title).toEqual('web-board.web-presentation');
  });

  it('should start SignalR connection on init', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    expect(mockSignalRService.startConnection).toHaveBeenCalled();
  });

  it('should stop SignalR connection on destroy', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    fixture.destroy();
    expect(mockSignalRService.stopConnection).toHaveBeenCalled();
  });
});
