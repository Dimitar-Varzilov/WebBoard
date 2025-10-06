import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function notInPastValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null; // Let required validator handle empty values
    }

    const inputDate = new Date(control.value);
    const now = new Date();

    if (inputDate <= now) {
      return { notInPast: { value: control.value } };
    }

    return null;
  };
}

export function minDateTimeValidator(minDateTime: Date): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const inputDate = new Date(control.value);

    if (inputDate < minDateTime) {
      return { minDateTime: { value: control.value, min: minDateTime } };
    }

    return null;
  };
}
