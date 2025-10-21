import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { RouterTestingModule } from '@angular/router/testing';
import { NavbarComponent } from './navbar.component';
import { ROUTES } from '../../../constants';
import { of } from 'rxjs';

describe('NavbarComponent', () => {
  let component: NavbarComponent;
  let fixture: ComponentFixture<NavbarComponent>;
  let compiled: HTMLElement;

  class MockAuthService {
    startPkceAuthWithGoogle = jasmine.createSpy('startPkceAuthWithGoogle');
    logout = jasmine
      .createSpy('logout')
      .and.returnValue({ subscribe: jasmine.createSpy('subscribe') });
    isAuthenticated$ = () => of(true); // default, can be overridden in tests
  }
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [NavbarComponent],
      imports: [RouterTestingModule],
      schemas: [NO_ERRORS_SCHEMA], // Ignore unknown elements
      providers: [
        {
          provide: require('../../../services/auth.service').AuthService,
          useClass: MockAuthService,
        },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(NavbarComponent);
    component = fixture.componentInstance;
    compiled = fixture.nativeElement;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should expose ROUTES constant', () => {
    expect(component.routes).toBe(ROUTES);
    expect(component.routes).toBeDefined();
  });

  describe('Routes Configuration', () => {
    it('should have dashboard route', () => {
      expect(component.routes.DASHBOARD).toBeDefined();
    });

    it('should have tasks route', () => {
      expect(component.routes.TASKS).toBeDefined();
    });

    it('should have task create route', () => {
      expect(component.routes.TASK_CREATE).toBeDefined();
    });

    it('should have jobs route', () => {
      expect(component.routes.JOBS).toBeDefined();
    });

    it('should have job create route', () => {
      expect(component.routes.JOB_CREATE).toBeDefined();
    });
  });

  describe('Template Rendering', () => {
    it('should render navbar element', () => {
      const navbar = compiled.querySelector('nav');
      expect(navbar).toBeTruthy();
    });

    it('should show logout button when authenticated', () => {
      (component.auth as any).isAuthenticated$ = () => of(true);
      fixture.detectChanges();
      const logoutBtn = compiled.querySelector('button.btn-outline-light');
      expect(logoutBtn).toBeTruthy();
      expect(logoutBtn?.textContent).toContain('Logout');
    });

    it('should not show logout button when not authenticated', () => {
      (component.auth as any).isAuthenticated$ = () => of(false);
      fixture.detectChanges();
      const logoutBtn = compiled.querySelector('button.btn-outline-light');
      expect(logoutBtn).toBeFalsy();
    });
  });

  describe('Methods', () => {
    it('should call startPkceAuthWithGoogle with correct redirectUri when signInWithGoogle is called', () => {
      const authService = component.auth as any;
      const expectedRedirectUri = window.location.origin + '/auth-callback';
      component.signInWithGoogle();
      expect(authService.startPkceAuthWithGoogle).toHaveBeenCalledWith(
        expectedRedirectUri
      );
    });

    it('should call logout and subscribe when logout is called', () => {
      const authService = component.auth as any;
      component.logout();
      expect(authService.logout).toHaveBeenCalled();
      expect(authService.logout().subscribe).toHaveBeenCalled();
    });
  });
});
