import { HttpParams } from '@angular/common/http';

/**
 * Utility class for building HttpParams in a clean, readable way
 * Eliminates the need for multiple if statements when constructing query parameters
 *
 * @example
 * // Instead of:
 * let params = new HttpParams();
 * if (value1) params = params.set('key1', value1);
 * if (value2) params = params.set('key2', value2);
 *
 * // Use:
 * const params = HttpParamsBuilder.build({
 *   key1: value1,
 *   key2: value2,
 *   key3: value3
 * });
 */
export class HttpParamsBuilder {
  /**
   * Build HttpParams from an object, automatically filtering out undefined/null values
   *
   * @param params - Object with key-value pairs to convert to HttpParams
   * @returns HttpParams instance with all defined values
   */
  static build(params: Record<string, any>): HttpParams {
    return Object.entries(params).reduce((httpParams, [key, value]) => {
      // Skip undefined and null values
      if (value === undefined || value === null) {
        return httpParams;
      }

      // Convert value to string (handles numbers, booleans, etc.)
      return httpParams.set(key, String(value));
    }, new HttpParams());
  }

  /**
   * Build HttpParams from an object with custom value transformation
   * Useful when you need special handling for certain types
   *
   * @param params - Object with key-value pairs
   * @param transform - Optional function to transform values before adding them
   * @returns HttpParams instance
   *
   * @example
   * const params = HttpParamsBuilder.buildWithTransform(
   *   { date: new Date(), active: true },
   *   (key, value) => value instanceof Date ? value.toISOString() : value
   * );
   */
  static buildWithTransform(
    params: Record<string, any>,
    transform?: (key: string, value: any) => any
  ): HttpParams {
    return Object.entries(params).reduce((httpParams, [key, value]) => {
      if (value === undefined || value === null) {
        return httpParams;
      }

      const transformedValue = transform ? transform(key, value) : value;
      return httpParams.set(key, String(transformedValue));
    }, new HttpParams());
  }

  /**
   * Build HttpParams from query parameters interface
   * Type-safe way to build params from typed query parameter objects
   *
   * @param queryParams - Query parameters object (e.g., TaskQueryParameters)
   * @returns HttpParams instance
   *
   * @example
   * const params = HttpParamsBuilder.fromQueryParams<TaskQueryParameters>({
   *   pageNumber: 1,
   *   pageSize: 25,
   *   sortBy: 'createdAt',
   *   sortDirection: 'desc',
   *   status: TaskItemStatus.InProgress
   * });
   */
  static fromQueryParams<T extends Record<string, any>>(
    queryParams: T
  ): HttpParams {
    return this.build(queryParams as Record<string, any>);
  }

  /**
   * Append multiple parameters to existing HttpParams
   *
   * @param existingParams - Existing HttpParams instance
   * @param newParams - Object with new parameters to add
   * @returns Updated HttpParams instance
   */
  static append(
    existingParams: HttpParams,
    newParams: Record<string, any>
  ): HttpParams {
    return Object.entries(newParams).reduce((httpParams, [key, value]) => {
      if (value === undefined || value === null) {
        return httpParams;
      }
      return httpParams.set(key, String(value));
    }, existingParams);
  }

  /**
   * Build HttpParams with array support
   * Useful for multi-select filters
   *
   * @param params - Object with key-value pairs, values can be arrays
   * @returns HttpParams instance
   *
   * @example
   * const params = HttpParamsBuilder.buildWithArrays({
   *   status: [1, 2, 3],
   *   tags: ['urgent', 'important']
   * });
   * // Results in: ?status=1&status=2&status=3&tags=urgent&tags=important
   */
  static buildWithArrays(params: Record<string, any>): HttpParams {
    return Object.entries(params).reduce((httpParams, [key, value]) => {
      if (value === undefined || value === null) {
        return httpParams;
      }

      // Handle arrays by adding multiple parameters with the same key
      if (Array.isArray(value)) {
        return value.reduce(
          (params, item) => params.append(key, String(item)),
          httpParams
        );
      }

      return httpParams.set(key, String(value));
    }, new HttpParams());
  }
}

/**
 * Functional helper for building HttpParams
 * Alternative to class-based approach
 */
export const buildHttpParams = (params: Record<string, any>): HttpParams => {
  return HttpParamsBuilder.build(params);
};

/**
 * Type guard to check if a value should be included in params
 */
export const isDefined = <T>(value: T | null | undefined): value is T => {
  return value !== null && value !== undefined;
};
