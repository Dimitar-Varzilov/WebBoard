import { DateTimeUtils } from './datetime.utils';

describe('DateTimeUtils', () => {
  beforeEach(() => {
    // Mock the current date to ensure consistent test results
    jasmine.clock().install();
    jasmine.clock().mockDate(new Date('2025-01-15T12:00:00Z'));
  });

  afterEach(() => {
    jasmine.clock().uninstall();
  });

  describe('localInputToUtcIso', () => {
    it('should convert local datetime to UTC ISO string', () => {
      const localDateTime = '2025-01-15T12:00';
      const result = DateTimeUtils.localInputToUtcIso(localDateTime);

      expect(result).toBeTruthy();
      expect(result).toContain('2025-01-15');
      expect(result).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/);
    });

    it('should return empty string for empty input', () => {
      expect(DateTimeUtils.localInputToUtcIso('')).toBe('');
    });

    it('should handle valid datetime-local format', () => {
      const localDateTime = '2025-03-20T15:30';
      const result = DateTimeUtils.localInputToUtcIso(localDateTime);

      expect(result).toBeTruthy();
      expect(result).toContain('T');
      expect(result).toContain('Z');
    });
  });

  describe('utcIsoToLocalInput', () => {
    it('should convert UTC ISO string to local input format', () => {
      const utcIso = '2025-01-15T12:00:00Z';
      const result = DateTimeUtils.utcIsoToLocalInput(utcIso);

      expect(result).toBeTruthy();
      expect(result).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/);
      expect(result.length).toBe(16); // YYYY-MM-DDTHH:mm
    });

    it('should return empty string for empty input', () => {
      expect(DateTimeUtils.utcIsoToLocalInput('')).toBe('');
    });

    it('should handle timezone conversion correctly', () => {
      const utcIso = '2025-06-15T00:00:00Z';
      const result = DateTimeUtils.utcIsoToLocalInput(utcIso);

      expect(result).toBeTruthy();
      expect(result).toContain('2025-06-');
    });
  });

  describe('getCurrentLocalInput', () => {
    it('should return current datetime in local input format', () => {
      const result = DateTimeUtils.getCurrentLocalInput();

      expect(result).toBeTruthy();
      expect(result).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/);
      expect(result.length).toBe(16);
    });

    it('should return time close to current time', () => {
      const result = DateTimeUtils.getCurrentLocalInput();
      const currentYear = new Date().getFullYear();

      expect(result).toContain(currentYear.toString());
    });
  });

  describe('formatForDisplay', () => {
    it('should format UTC date for display with default locale', () => {
      const utcIso = '2025-01-15T12:00:00Z';
      const result = DateTimeUtils.formatForDisplay(utcIso);

      expect(result).toBeTruthy();
      expect(result).toContain('2025');
      expect(result).toContain('Jan');
    });

    it('should return empty string for empty input', () => {
      expect(DateTimeUtils.formatForDisplay('')).toBe('');
    });

    it('should use custom locale when provided', () => {
      const utcIso = '2025-01-15T12:00:00Z';
      const result = DateTimeUtils.formatForDisplay(utcIso, 'en-GB');

      expect(result).toBeTruthy();
    });

    it('should use custom options when provided', () => {
      const utcIso = '2025-01-15T12:00:00Z';
      const result = DateTimeUtils.formatForDisplay(utcIso, 'en-US', {
        month: 'long',
        day: 'numeric',
      });

      expect(result).toBeTruthy();
      expect(result).toContain('January');
    });
  });

  describe('formatCompact', () => {
    it('should format date compactly without timezone', () => {
      const utcIso = '2025-01-15T12:00:00Z';
      const result = DateTimeUtils.formatCompact(utcIso);

      expect(result).toBeTruthy();
      expect(result).toContain('Jan');
      expect(result).toContain('15');
    });

    it('should return empty string for empty input', () => {
      expect(DateTimeUtils.formatCompact('')).toBe('');
    });

    it('should include hour and minute', () => {
      const utcIso = '2025-01-15T14:30:00Z';
      const result = DateTimeUtils.formatCompact(utcIso);

      expect(result).toBeTruthy();
      expect(result).toMatch(/\d{1,2}:\d{2}/);
    });
  });

  describe('formatRelative', () => {
    it('should return "just now" for current time', () => {
      const now = new Date('2025-01-15T12:00:00Z');
      const result = DateTimeUtils.formatRelative(now.toISOString());

      expect(result).toBe('just now');
    });

    it('should return "X minutes ago" for recent past', () => {
      const pastTime = new Date('2025-01-15T11:45:00Z'); // 15 minutes ago
      const result = DateTimeUtils.formatRelative(pastTime.toISOString());

      expect(result).toContain('minutes ago');
      expect(result).toContain('15');
    });

    it('should return "in X minutes" for near future', () => {
      const futureTime = new Date('2025-01-15T12:30:00Z'); // 30 minutes from now
      const result = DateTimeUtils.formatRelative(futureTime.toISOString());

      expect(result).toContain('in');
      expect(result).toContain('minutes');
    });

    it('should return "X hours ago" for several hours past', () => {
      const pastTime = new Date('2025-01-15T09:00:00Z'); // 3 hours ago
      const result = DateTimeUtils.formatRelative(pastTime.toISOString());

      expect(result).toContain('hours ago');
      expect(result).toContain('3');
    });

    it('should return "in X hours" for several hours future', () => {
      const futureTime = new Date('2025-01-15T17:00:00Z'); // 5 hours from now
      const result = DateTimeUtils.formatRelative(futureTime.toISOString());

      expect(result).toContain('in');
      expect(result).toContain('hours');
    });

    it('should return "X days ago" for past days', () => {
      const pastTime = new Date('2025-01-13T12:00:00Z'); // 2 days ago
      const result = DateTimeUtils.formatRelative(pastTime.toISOString());

      expect(result).toContain('days ago');
      expect(result).toContain('2');
    });

    it('should return compact format for distant dates', () => {
      const distantPast = new Date('2024-12-01T12:00:00Z'); // Over a week ago
      const result = DateTimeUtils.formatRelative(distantPast.toISOString());

      expect(result).toBeTruthy();
      expect(result).toContain('Dec');
    });

    it('should return empty string for empty input', () => {
      expect(DateTimeUtils.formatRelative('')).toBe('');
    });
  });

  describe('isPast', () => {
    it('should return true for past dates', () => {
      const pastTime = new Date('2025-01-15T11:00:00Z').toISOString();
      expect(DateTimeUtils.isPast(pastTime)).toBe(true);
    });

    it('should return false for future dates', () => {
      const futureTime = new Date('2025-01-15T13:00:00Z').toISOString();
      expect(DateTimeUtils.isPast(futureTime)).toBe(false);
    });

    it('should return false for empty input', () => {
      expect(DateTimeUtils.isPast('')).toBe(false);
    });
  });

  describe('isFuture', () => {
    it('should return true for future dates', () => {
      const futureTime = new Date('2025-01-15T13:00:00Z').toISOString();
      expect(DateTimeUtils.isFuture(futureTime)).toBe(true);
    });

    it('should return false for past dates', () => {
      const pastTime = new Date('2025-01-15T11:00:00Z').toISOString();
      expect(DateTimeUtils.isFuture(pastTime)).toBe(false);
    });

    it('should return false for empty input', () => {
      expect(DateTimeUtils.isFuture('')).toBe(false);
    });
  });

  describe('addTime', () => {
    it('should add minutes correctly', () => {
      const baseTime = '2025-01-15T12:00:00Z';
      const result = DateTimeUtils.addTime(baseTime, 30, 'minutes');

      expect(result).toBeTruthy();
      expect(new Date(result).getMinutes()).toBe(30);
    });

    it('should add hours correctly', () => {
      const baseTime = '2025-01-15T12:00:00Z';
      const result = DateTimeUtils.addTime(baseTime, 3, 'hours');

      expect(result).toBeTruthy();
      expect(new Date(result).getUTCHours()).toBe(15);
    });

    it('should add days correctly', () => {
      const baseTime = '2025-01-15T12:00:00Z';
      const result = DateTimeUtils.addTime(baseTime, 5, 'days');

      expect(result).toBeTruthy();
      expect(new Date(result).getDate()).toBe(20);
    });

    it('should handle negative values (subtract time)', () => {
      const baseTime = '2025-01-15T12:00:00Z';
      const result = DateTimeUtils.addTime(baseTime, -2, 'hours');

      expect(result).toBeTruthy();
      expect(new Date(result).getUTCHours()).toBe(10);
    });

    it('should return empty string for empty input', () => {
      expect(DateTimeUtils.addTime('', 5, 'minutes')).toBe('');
    });
  });

  describe('getCurrentTimezoneOffset', () => {
    it('should return timezone offset in correct format', () => {
      const result = DateTimeUtils.getCurrentTimezoneOffset();

      expect(result).toMatch(/^[+-]\d{2}:\d{2}$/);
    });

    it('should include sign character', () => {
      const result = DateTimeUtils.getCurrentTimezoneOffset();

      expect(result[0]).toMatch(/[+-]/);
    });

    it('should have correct length', () => {
      const result = DateTimeUtils.getCurrentTimezoneOffset();

      expect(result.length).toBe(6); // e.g., "+05:30" or "-08:00"
    });
  });

  describe('getCurrentTimezoneName', () => {
    it('should return a valid timezone name', () => {
      const result = DateTimeUtils.getCurrentTimezoneName();

      expect(result).toBeTruthy();
      expect(typeof result).toBe('string');
    });

    it('should return IANA timezone identifier', () => {
      const result = DateTimeUtils.getCurrentTimezoneName();

      // Common timezone patterns
      expect(result).toMatch(/^[A-Za-z_]+\/[A-Za-z_]+$/);
    });
  });
});
