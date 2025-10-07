using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WebBoard.API.Common.DTOs.Common;

namespace WebBoard.API.Common.Extensions
{
	/// <summary>
	/// Extension methods for building dynamic queries with pagination, filtering, and sorting
	/// </summary>
	public static class QueryExtensions
	{
		/// <summary>
		/// Apply pagination to a query
		/// </summary>
		public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, QueryParameters parameters)
		{
			return query
				.Skip((parameters.PageNumber - 1) * parameters.PageSize)
				.Take(parameters.PageSize);
		}

		/// <summary>
		/// Apply sorting to a query using property name
		/// </summary>
		public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, string? sortBy, bool isAscending)
		{
			if (string.IsNullOrWhiteSpace(sortBy))
				return query;

			// Normalize property name (case-insensitive)
			var propertyInfo = typeof(T).GetProperty(
				sortBy,
				System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
			);

			if (propertyInfo == null)
				return query;

			var parameter = Expression.Parameter(typeof(T), "x");
			var property = Expression.Property(parameter, propertyInfo);
			var lambda = Expression.Lambda(property, parameter);

			var methodName = isAscending ? "OrderBy" : "OrderByDescending";
			var resultExpression = Expression.Call(
				typeof(Queryable),
				methodName,
				[typeof(T), propertyInfo.PropertyType],
				query.Expression,
				Expression.Quote(lambda)
			);

			return query.Provider.CreateQuery<T>(resultExpression);
		}

		/// <summary>
		/// Create a paginated result from a query
		/// </summary>
		public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
			this IQueryable<T> query,
			QueryParameters parameters,
			CancellationToken cancellationToken = default)
		{
			var totalCount = await query.CountAsync(cancellationToken);

			var items = await query
				.ApplyPagination(parameters)
				.ToListAsync(cancellationToken);

			return new PagedResult<T>(items, totalCount, parameters.PageNumber, parameters.PageSize);
		}
	}
}
