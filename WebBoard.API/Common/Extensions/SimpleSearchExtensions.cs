using Microsoft.EntityFrameworkCore;

namespace WebBoard.API.Common.Extensions
{
	/// <summary>
	/// Simple, maintainable search extensions for PostgreSQL
	/// Provides clear, explicit methods for common search scenarios
	/// </summary>
	public static class SimpleSearchExtensions
	{
		/// <summary>
		/// Apply PostgreSQL case-insensitive search to multiple string fields
		/// IMPORTANT: Explicitly list all searchable fields to ensure maintainability
		/// </summary>
		/// <typeparam name="T">Entity type</typeparam>
		/// <param name="query">Source query</param>
		/// <param name="searchTerm">Search term</param>
		/// <param name="fieldSelectors">Functions that select string fields to search</param>
		/// <returns>Filtered query</returns>
		/// <example>
		/// query.SearchInFields(searchTerm, 
		///     j => j.JobType, 
		///     j => j.Description, 
		///     j => j.Department
		/// );
		/// </example>
		public static IQueryable<T> SearchInFields<T>(
			this IQueryable<T> query,
			string? searchTerm,
			params Func<T, string>[] fieldSelectors) where T : class
		{
			if (string.IsNullOrWhiteSpace(searchTerm) || fieldSelectors.Length == 0)
				return query;

			// Build OR condition for all fields
			// Example: field1 ILIKE '%term%' OR field2 ILIKE '%term%' OR field3 ILIKE '%term%'
			return query.Where(entity =>
				fieldSelectors.Any(selector =>
					EF.Functions.ILike(selector(entity), $"%{searchTerm}%")
				)
			);
		}

		/// <summary>
		/// Apply search with null-safe field handling
		/// Automatically handles nullable string fields
		/// </summary>
		public static IQueryable<T> SearchInNullableFields<T>(
			this IQueryable<T> query,
			string? searchTerm,
			params Func<T, string?>[] fieldSelectors) where T : class
		{
			if (string.IsNullOrWhiteSpace(searchTerm) || fieldSelectors.Length == 0)
				return query;

			return query.Where(entity =>
				fieldSelectors.Any(selector =>
					selector(entity) != null &&
					EF.Functions.ILike(selector(entity)!, $"%{searchTerm}%")
				)
			);
		}
	}
}
