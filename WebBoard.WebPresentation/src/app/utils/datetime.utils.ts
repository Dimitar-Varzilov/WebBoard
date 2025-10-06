/**
 * Comprehensive DateTime utilities for timezone-aware applications
 * Handles conversion between UTC (backend) and local time (user display)
 */
export class DateTimeUtils {
  /**
   * Convert local datetime-local input to UTC ISO string for API
   * @param localDateTime string from datetime-local input
   * @returns UTC ISO 8601 string
   */
  static localInputToUtcIso(localDateTime: string): string {
    if (!localDateTime) return '';
    
    const localDate = new Date(localDateTime);
    return localDate.toISOString();
  }

  /**
   * Convert UTC ISO string from API to local datetime-local input format
   * @param utcIsoString UTC ISO 8601 string from API
   * @returns Local datetime string for input[type="datetime-local"]
   */
  static utcIsoToLocalInput(utcIsoString: string): string {
    if (!utcIsoString) return '';
    
    const utcDate = new Date(utcIsoString);
    const localDate = new Date(utcDate.getTime() - (utcDate.getTimezoneOffset() * 60000));
    return localDate.toISOString().slice(0, 16);
  }

  /**
   * Get current datetime in local input format (for min attribute)
   * @returns Current local time in datetime-local format
   */
  static getCurrentLocalInput(): string {
    const now = new Date();
    const localNow = new Date(now.getTime() - (now.getTimezoneOffset() * 60000));
    return localNow.toISOString().slice(0, 16);
  }

  /**
   * Format UTC date for user display in local timezone
   * @param utcIsoString UTC ISO 8601 string
   * @param locale Locale for formatting (default: 'en-US')
   * @param options Intl.DateTimeFormatOptions
   * @returns Formatted date string in user's timezone
   */
  static formatForDisplay(
    utcIsoString: string, 
    locale: string = 'en-US',
    options?: Intl.DateTimeFormatOptions
  ): string {
    if (!utcIsoString) return '';
    
    const date = new Date(utcIsoString);
    const defaultOptions: Intl.DateTimeFormatOptions = {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      timeZoneName: 'short'
    };
    
    return date.toLocaleString(locale, options || defaultOptions);
  }

  /**
   * Format date for compact display (no timezone)
   * @param utcIsoString UTC ISO 8601 string
   * @param locale Locale for formatting
   * @returns Compact formatted date string
   */
  static formatCompact(utcIsoString: string, locale: string = 'en-US'): string {
    if (!utcIsoString) return '';
    
    const date = new Date(utcIsoString);
    return date.toLocaleString(locale, {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Format relative time (e.g., "2 hours ago", "in 30 minutes")
   * @param utcIsoString UTC ISO 8601 string
   * @param locale Locale for formatting
   * @returns Relative time string
   */
  static formatRelative(utcIsoString: string, locale: string = 'en-US'): string {
    if (!utcIsoString) return '';
    
    const date = new Date(utcIsoString);
    const now = new Date();
    const diffMs = date.getTime() - now.getTime();
    const diffMinutes = Math.round(diffMs / (1000 * 60));
    const diffHours = Math.round(diffMs / (1000 * 60 * 60));
    const diffDays = Math.round(diffMs / (1000 * 60 * 60 * 24));

    if (Math.abs(diffMinutes) < 1) {
      return 'just now';
    } else if (Math.abs(diffMinutes) < 60) {
      return diffMinutes > 0 ? `in ${diffMinutes} minutes` : `${Math.abs(diffMinutes)} minutes ago`;
    } else if (Math.abs(diffHours) < 24) {
      return diffHours > 0 ? `in ${diffHours} hours` : `${Math.abs(diffHours)} hours ago`;
    } else if (Math.abs(diffDays) < 7) {
      return diffDays > 0 ? `in ${diffDays} days` : `${Math.abs(diffDays)} days ago`;
    } else {
      return this.formatCompact(utcIsoString, locale);
    }
  }

  /**
   * Check if a UTC ISO string represents a past time
   * @param utcIsoString UTC ISO 8601 string
   * @returns true if the time is in the past
   */
  static isPast(utcIsoString: string): boolean {
    if (!utcIsoString) return false;
    
    const date = new Date(utcIsoString);
    return date.getTime() < Date.now();
  }

  /**
   * Check if a UTC ISO string represents a future time
   * @param utcIsoString UTC ISO 8601 string
   * @returns true if the time is in the future
   */
  static isFuture(utcIsoString: string): boolean {
    if (!utcIsoString) return false;
    
    const date = new Date(utcIsoString);
    return date.getTime() > Date.now();
  }

  /**
   * Add time to a UTC ISO string
   * @param utcIsoString UTC ISO 8601 string
   * @param amount Amount to add
   * @param unit Time unit ('minutes', 'hours', 'days')
   * @returns New UTC ISO 8601 string
   */
  static addTime(utcIsoString: string, amount: number, unit: 'minutes' | 'hours' | 'days'): string {
    if (!utcIsoString) return '';
    
    const date = new Date(utcIsoString);
    
    switch (unit) {
      case 'minutes':
        date.setMinutes(date.getMinutes() + amount);
        break;
      case 'hours':
        date.setHours(date.getHours() + amount);
        break;
      case 'days':
        date.setDate(date.getDate() + amount);
        break;
    }
    
    return date.toISOString();
  }

  /**
   * Get timezone offset string for current user
   * @returns Timezone offset (e.g., "-05:00", "+09:00")
   */
  static getCurrentTimezoneOffset(): string {
    const now = new Date();
    const offset = -now.getTimezoneOffset();
    const hours = Math.floor(Math.abs(offset) / 60);
    const minutes = Math.abs(offset) % 60;
    const sign = offset >= 0 ? '+' : '-';
    
    return `${sign}${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`;
  }

  /**
   * Get timezone name for current user
   * @returns Timezone name (e.g., "EST", "PST", "JST")
   */
  static getCurrentTimezoneName(): string {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
  }
}
