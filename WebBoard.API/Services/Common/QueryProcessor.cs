using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;
using WebBoard.API.Common.DTOs.Common;

namespace WebBoard.API.Services.Common
{
	/// <summary>
	/// Provides generic filtering, sorting, and pagination for IQueryable sources using Sieve.
	/// </summary>
	public class QueryProcessor(ISieveProcessor sieveProcessor, IOptions<SieveOptions> sieveOptions) : IQueryProcessor
	{
		public async Task<PagedResult<TDto>> ApplyAsync<TEntity, TDto>(
			IQueryable<TEntity> query,
			QueryParameters parameters,
			Func<TEntity, TDto> selector)
		{
			// Get total count before pagination
			var totalCount = await query.CountAsync();

			// Apply Sieve for sorting and pagination
			var pagedQuery = sieveProcessor.Apply(parameters, query);
            var items = await pagedQuery
                .ToListAsync();
            var itemsDto = items.Select(selector);

			return new PagedResult<TDto>(itemsDto, totalCount, parameters.Page ?? 1, parameters.PageSize ?? sieveOptions.Value.DefaultPageSize);
		}
	}
}
