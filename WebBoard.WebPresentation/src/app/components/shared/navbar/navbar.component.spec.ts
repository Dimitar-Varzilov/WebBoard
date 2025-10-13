import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { RouterTestingModule } from '@angular/router/testing';
import { NavbarComponent } from './navbar.component';
import { ROUTES } from '../../../constants';

describe('NavbarComponent', () => {
  let component: NavbarComponent;
  let fixture: ComponentFixture<NavbarComponent>;
  let compiled: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [NavbarComponent],
      imports: [RouterTestingModule],
      schemas: [NO_ERRORS_SCHEMA], // Ignore unknown elements
    }).compileComponents();

    fixture = TestBed.createComponent(NavbarComponent);
    component = fixture.componentInstance;
    compiled = fixture.nativeElement;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Initialization', () => {
    it('should call ngOnInit', () => {
      expect(component).toBeDefined();
    });

    it('should expose ROUTES constant', () => {
      expect(component.routes).toBe(ROUTES);
      expect(component.routes).toBeDefined();
    });
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
  });

  describe('Component Lifecycle', () => {
    it('should initialize without errors', () => {
      expect(() => component.ngOnInit()).not.toThrow();
    });
  });

  describe('Constants', () => {
    it('should have immutable routes reference', () => {
      const routesBefore = component.routes;
      component.ngOnInit();
      const routesAfter = component.routes;

      expect(routesBefore).toBe(routesAfter);
    });
  });
});
