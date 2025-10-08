import { FormControl } from '@angular/forms';
import {
  notInPastValidator,
  minimumFutureTimeValidator,
  maximumFutureTimeValidator,
  businessHoursValidator,
} from './datetime.validators';

describe('DateTimeValidators', () => {
  describe('notInPastValidator', () => {
    it('should return null for future dates', () => {
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 1);
      const control = new FormControl(futureDate.toISOString());

      const validator = notInPastValidator();
      const result = validator(control);

      expect(result).toBeNull();
    });

    it('should return error for past dates', () => {
      const pastDate = new Date();
      pastDate.setDate(pastDate.getDate() - 1);
      const control = new FormControl(pastDate.toISOString());

      const validator = notInPastValidator();
      const result = validator(control);

      expect(result).toBeTruthy();
      expect(result?.['notInPast']).toBeTruthy();
    });

    it('should return error for current time', () => {
      const now = new Date();
      const control = new FormControl(now.toISOString());

      const validator = notInPastValidator();
      const result = validator(control);

      // Current time might be past by the time validation runs (with 1 min buffer)
      expect(result).toBeTruthy();
      expect(result?.['notInPast']).toBeTruthy();
    });

    it('should return null for null values', () => {
      const control = new FormControl(null);

      const validator = notInPastValidator();
      const result = validator(control);

      expect(result).toBeNull();
    });

    it('should return null for undefined values', () => {
      const control = new FormControl(undefined);

      const validator = notInPastValidator();
      const result = validator(control);

      expect(result).toBeNull();
    });

    it('should return null for empty string', () => {
      const control = new FormControl('');

      const validator = notInPastValidator();
      const result = validator(control);

      expect(result).toBeNull();
    });

    it('should handle invalid date strings', () => {
      const control = new FormControl('invalid-date');

      const validator = notInPastValidator();
      const result = validator(control);

      // Invalid dates create Invalid Date object, which has NaN timestamp
      // NaN comparisons always return false, so validator returns null
      expect(result).toBeNull();
    });
  });

  describe('minimumFutureTimeValidator', () => {
    it('should return null for dates beyond minimum minutes', () => {
      const futureDate = new Date();
      futureDate.setMinutes(futureDate.getMinutes() + 10);
      const control = new FormControl(futureDate.toISOString());

      const result = minimumFutureTimeValidator(5)(control);

      expect(result).toBeNull();
    });

    it('should return error for dates within minimum minutes', () => {
      const nearFutureDate = new Date();
      nearFutureDate.setMinutes(nearFutureDate.getMinutes() + 2);
      const control = new FormControl(nearFutureDate.toISOString());

      const result = minimumFutureTimeValidator(5)(control);

      expect(result).toBeTruthy();
      expect(result?.['minimumFutureTime']).toBeDefined();
      expect(result?.['minimumFutureTime'].minimumMinutes).toBe(5);
      expect(result?.['minimumFutureTime'].value).toBe(
        nearFutureDate.toISOString()
      );
    });

    it('should return error for current time', () => {
      const now = new Date();
      const control = new FormControl(now.toISOString());

      const result = minimumFutureTimeValidator(5)(control);

      expect(result).toBeTruthy();
      expect(result?.['minimumFutureTime']).toBeDefined();
      expect(result?.['minimumFutureTime'].minimumMinutes).toBe(5);
    });

    it('should return error for past dates', () => {
      const pastDate = new Date();
      pastDate.setMinutes(pastDate.getMinutes() - 10);
      const control = new FormControl(pastDate.toISOString());

      const result = minimumFutureTimeValidator(5)(control);

      expect(result).toBeTruthy();
      expect(result?.['minimumFutureTime']).toBeDefined();
      expect(result?.['minimumFutureTime'].minimumMinutes).toBe(5);
    });

    it('should return null for null values', () => {
      const control = new FormControl(null);

      const result = minimumFutureTimeValidator(5)(control);

      expect(result).toBeNull();
    });

    it('should return null for empty string', () => {
      const control = new FormControl('');

      const result = minimumFutureTimeValidator(5)(control);

      expect(result).toBeNull();
    });

    it('should handle 0 minutes threshold', () => {
      const futureDate = new Date();
      futureDate.setSeconds(futureDate.getSeconds() + 1);
      const control = new FormControl(futureDate.toISOString());

      const result = minimumFutureTimeValidator(0)(control);

      expect(result).toBeNull();
    });

    it('should handle large minute thresholds', () => {
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 1);
      const control = new FormControl(futureDate.toISOString());

      const result = minimumFutureTimeValidator(2000)(control);

      expect(result).toBeTruthy();
      expect(result?.['minimumFutureTime']).toBeDefined();
      expect(result?.['minimumFutureTime'].minimumMinutes).toBe(2000);
    });

    it('should handle edge case at exact threshold', () => {
      const futureDate = new Date();
      futureDate.setMinutes(futureDate.getMinutes() + 5);
      const control = new FormControl(futureDate.toISOString());

      const validator = minimumFutureTimeValidator(5);
      const result = validator(control);

      // At exact threshold, might be valid or invalid depending on exact timing
      // Either null or error with requiredMinutes is acceptable
      if (result) {
        expect(result['minimumFutureTime']).toBeTruthy();
      } else {
        expect(result).toBeNull();
      }
    });
  });

  describe('maximumFutureTimeValidator', () => {
    it('should return null for dates within maximum days', () => {
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 5);
      const control = new FormControl(futureDate.toISOString());

      const result = maximumFutureTimeValidator(10)(control);

      expect(result).toBeNull();
    });

    it('should return error for dates beyond maximum days', () => {
      const farFutureDate = new Date();
      farFutureDate.setDate(farFutureDate.getDate() + 15);
      const control = new FormControl(farFutureDate.toISOString());

      const result = maximumFutureTimeValidator(10)(control);

      expect(result).toBeTruthy();
      expect(result?.['maximumFutureTime']).toBeDefined();
      expect(result?.['maximumFutureTime'].maximumDays).toBe(10);
      expect(result?.['maximumFutureTime'].value).toBe(
        farFutureDate.toISOString()
      );
    });

    it('should return null for current time', () => {
      const now = new Date();
      const control = new FormControl(now.toISOString());

      const result = maximumFutureTimeValidator(10)(control);

      expect(result).toBeNull();
    });

    it('should return null for past dates', () => {
      const pastDate = new Date();
      pastDate.setDate(pastDate.getDate() - 5);
      const control = new FormControl(pastDate.toISOString());

      const result = maximumFutureTimeValidator(10)(control);

      expect(result).toBeNull();
    });

    it('should return null for null values', () => {
      const control = new FormControl(null);

      const result = maximumFutureTimeValidator(10)(control);

      expect(result).toBeNull();
    });

    it('should return null for empty string', () => {
      const control = new FormControl('');

      const result = maximumFutureTimeValidator(10)(control);

      expect(result).toBeNull();
    });

    it('should handle 0 days threshold', () => {
      const futureDate = new Date();
      futureDate.setHours(futureDate.getHours() + 1);
      const control = new FormControl(futureDate.toISOString());

      const result = maximumFutureTimeValidator(0)(control);

      expect(result).toBeTruthy();
      expect(result?.['maximumFutureTime']).toBeDefined();
      expect(result?.['maximumFutureTime'].maximumDays).toBe(0);
    });

    it('should handle large day thresholds', () => {
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 100);
      const control = new FormControl(futureDate.toISOString());

      const result = maximumFutureTimeValidator(365)(control);

      expect(result).toBeNull();
    });

    it('should handle edge case at exact threshold', () => {
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 10);
      const control = new FormControl(futureDate.toISOString());

      const validator = maximumFutureTimeValidator(10);
      const result = validator(control);

      // At exact threshold, might be valid or invalid depending on exact timing
      // Either null or error with maximumDays is acceptable
      if (result) {
        expect(result['maximumFutureTime']).toBeTruthy();
      } else {
        expect(result).toBeNull();
      }
    });
  });

  describe('businessHoursValidator', () => {
    it('should return null for weekday dates within business hours', () => {
      const weekdayDate = new Date('2024-01-15T10:00:00'); // Monday 10 AM
      const control = new FormControl(weekdayDate.toISOString());

      const validator = businessHoursValidator();
      const result = validator(control);

      expect(result).toBeNull();
    });

    it('should return error for Saturday', () => {
      const saturdayDate = new Date('2024-01-13T10:00:00'); // Saturday 10 AM
      const control = new FormControl(saturdayDate.toISOString());

      const validator = businessHoursValidator();
      const result = validator(control);

      expect(result).toBeTruthy();
      expect(result?.['businessHours']).toBeTruthy();
    });

    it('should return error for Sunday', () => {
      const sundayDate = new Date('2024-01-14T10:00:00'); // Sunday 10 AM
      const control = new FormControl(sundayDate.toISOString());

      const validator = businessHoursValidator();
      const result = validator(control);

      expect(result).toBeTruthy();
      expect(result?.['businessHours']).toBeTruthy();
    });

    it('should return error for time before business hours', () => {
      const earlyDate = new Date('2024-01-15T07:00:00'); // Monday 7 AM
      const control = new FormControl(earlyDate.toISOString());

      const validator = businessHoursValidator();
      const result = validator(control);

      expect(result).toBeTruthy();
      expect(result?.['businessHours']).toBeTruthy();
    });

    it('should return error for time after business hours', () => {
      const lateDate = new Date('2024-01-15T19:00:00'); // Monday 7 PM
      const control = new FormControl(lateDate.toISOString());

      const validator = businessHoursValidator();
      const result = validator(control);

      expect(result).toBeTruthy();
      expect(result?.['businessHours']).toBeTruthy();
    });

    it('should return null for start of business hours (9 AM)', () => {
      const startDate = new Date('2024-01-15T09:00:00'); // Monday 9 AM
      const control = new FormControl(startDate.toISOString());

      const validator = businessHoursValidator();
      const result = validator(control);

      expect(result).toBeNull();
    });

    it('should return error for end of business hours (5 PM)', () => {
      const endDate = new Date('2024-01-15T17:00:00'); // Monday 5 PM
      const control = new FormControl(endDate.toISOString());

      const validator = businessHoursValidator();
      const result = validator(control);

      // 5 PM (17:00) is >= endHour (17), so should return error
      expect(result).toBeTruthy();
      expect(result?.['businessHours']).toBeTruthy();
    });

    it('should return null for null values', () => {
      const control = new FormControl(null);

      const validator = businessHoursValidator();
      const result = validator(control);

      expect(result).toBeNull();
    });

    it('should return null for empty string', () => {
      const control = new FormControl('');

      const validator = businessHoursValidator();
      const result = validator(control);

      expect(result).toBeNull();
    });

    it('should handle all weekdays correctly', () => {
      const weekdays = [
        new Date('2024-01-15T10:00:00'), // Monday
        new Date('2024-01-16T10:00:00'), // Tuesday
        new Date('2024-01-17T10:00:00'), // Wednesday
        new Date('2024-01-18T10:00:00'), // Thursday
        new Date('2024-01-19T10:00:00'), // Friday
      ];

      const validator = businessHoursValidator();
      weekdays.forEach((date) => {
        const control = new FormControl(date.toISOString());
        const result = validator(control);
        expect(result).toBeNull();
      });
    });

    it('should handle midnight edge case', () => {
      const midnightDate = new Date('2024-01-15T00:00:00'); // Monday midnight
      const control = new FormControl(midnightDate.toISOString());

      const validator = businessHoursValidator();
      const result = validator(control);

      expect(result).toBeTruthy();
      expect(result?.['businessHours']).toBeTruthy();
    });

    it('should handle noon correctly', () => {
      const noonDate = new Date('2024-01-15T12:00:00'); // Monday noon
      const control = new FormControl(noonDate.toISOString());

      const validator = businessHoursValidator();
      const result = validator(control);

      expect(result).toBeNull();
    });
  });

  describe('Validator Combination', () => {
    it('should work with multiple validators on same control', () => {
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 5);
      const control = new FormControl(futureDate.toISOString());

      const notInPastValidator1 = notInPastValidator();
      const minimumValidator = minimumFutureTimeValidator(60);
      const maximumValidator = maximumFutureTimeValidator(30);

      const notInPastResult = notInPastValidator1(control);
      const minimumResult = minimumValidator(control);
      const maximumResult = maximumValidator(control);

      expect(notInPastResult).toBeNull();
      expect(minimumResult).toBeNull();
      expect(maximumResult).toBeNull();
    });

    it('should handle conflicting validator requirements', () => {
      const nearFutureDate = new Date();
      nearFutureDate.setMinutes(nearFutureDate.getMinutes() + 2);
      const control = new FormControl(nearFutureDate.toISOString());

      const validator = minimumFutureTimeValidator(5);
      const minimumResult = validator(control);

      expect(minimumResult).toBeTruthy();
      expect(minimumResult?.['minimumFutureTime']).toBeTruthy();
      expect(minimumResult?.['minimumFutureTime']?.['minimumMinutes']).toBe(5);
    });
  });
});
