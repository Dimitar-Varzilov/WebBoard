import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Validator that ensures a date/time is not in the past
 * Handles timezone conversion properly for consistent validation
 * Works with datetime-local inputs and UTC API responses
 */
export function notInPastValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null; // Let required validator handle empty values
    }

    // Convert input to Date object and compare with current time
    const inputDate = new Date(control.value);
    const now = new Date();

    // Add small buffer to account for processing time
    const nowWithBuffer = new Date(now.getTime() + (1000 * 60)); // 1 minute buffer

    if (inputDate.getTime() <= nowWithBuffer.getTime()) {
      return { 
        notInPast: { 
          value: control.value, 
          actualDate: inputDate.toISOString(),
          minimumDate: nowWithBuffer.toISOString()
        } 
      };
    }

    return null;
  };
}

/**
 * Validator that ensures a date is at least X minutes in the future
 * Useful for scheduling systems that need buffer time
 */
export function minimumFutureTimeValidator(minimumMinutes: number = 5): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const inputDate = new Date(control.value);
    const minimumTime = new Date(Date.now() + (minimumMinutes * 60 * 1000));

    if (inputDate.getTime() < minimumTime.getTime()) {
      return { 
        minimumFutureTime: { 
          value: control.value, 
          minimumMinutes,
          requiredTime: minimumTime.toISOString(),
          actualTime: inputDate.toISOString()
        } 
      };
    }

    return null;
  };
}

/**
 * Validator that ensures a date is within a reasonable future range
 * Prevents users from scheduling jobs too far in the future
 */
export function maximumFutureTimeValidator(maximumDays: number = 365): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const inputDate = new Date(control.value);
    const maximumTime = new Date(Date.now() + (maximumDays * 24 * 60 * 60 * 1000));

    if (inputDate.getTime() > maximumTime.getTime()) {
      return { 
        maximumFutureTime: { 
          value: control.value, 
          maximumDays,
          maximumTime: maximumTime.toISOString(),
          actualTime: inputDate.toISOString()
        } 
      };
    }

    return null;
  };
}

/**
 * Validator for business hours (optional)
 * Ensures scheduled time falls within business hours
 */
export function businessHoursValidator(
  startHour: number = 9, 
  endHour: number = 17,
  allowWeekends: boolean = false
): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const inputDate = new Date(control.value);
    const dayOfWeek = inputDate.getDay(); // 0 = Sunday, 6 = Saturday
    const hour = inputDate.getHours();

    // Check weekends
    if (!allowWeekends && (dayOfWeek === 0 || dayOfWeek === 6)) {
      return {
        businessHours: {
          value: control.value,
          error: 'Weekend scheduling not allowed',
          allowWeekends
        }
      };
    }

    // Check business hours
    if (hour < startHour || hour >= endHour) {
      return {
        businessHours: {
          value: control.value,
          error: `Must be between ${startHour}:00 and ${endHour}:00`,
          startHour,
          endHour,
          actualHour: hour
        }
      };
    }

    return null;
  };
}

/**
 * Utility functions for consistent date handling across the application
 */
export class DateTimeUtils {
  /**
   * Convert local datetime-local input to UTC ISO string for API
   */
  static localInputToUtcIso(localDateTime: string): string {
    const localDate = new Date(localDateTime);
    return localDate.toISOString();
  }

  /**
   * Convert UTC ISO string from API to local datetime-local input format
   */
  static utcIsoToLocalInput(utcIsoString: string): string {
    const utcDate = new Date(utcIsoString);
    const localDate = new Date(utcDate.getTime() - (utcDate.getTimezoneOffset() * 60000));
    return localDate.toISOString().slice(0, 16);
  }

  /**
   * Get current datetime in local input format (for min attribute)
   */
  static getCurrentLocalInput(): string {
    const now = new Date();
    const localNow = new Date(now.getTime() - (now.getTimezoneOffset() * 60000));
    return localNow.toISOString().slice(0, 16);
  }

  /**
   * Format UTC date for user display in local timezone
   */
  static formatForDisplay(utcIsoString: string, locale: string = 'en-US'): string {
    const date = new Date(utcIsoString);
    return date.toLocaleString(locale, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      timeZoneName: 'short'
    });
  }
}
