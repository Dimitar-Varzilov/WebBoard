using WebBoard.API.Common.DTOs.Common;

namespace WebBoard.API.Services.Common
{
	public interface IQueryProcessor
	{
		Task<PagedResult<TDto>> ApplyAsync<TEntity, TDto>(IQueryable<TEntity> query, QueryParameters parameters, Func<TEntity, TDto> selector);
	}
}