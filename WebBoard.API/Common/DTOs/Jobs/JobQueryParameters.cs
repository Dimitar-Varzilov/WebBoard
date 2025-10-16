using WebBoard.API.Common.DTOs.Common;

namespace WebBoard.API.Common.DTOs.Jobs
{
using Sieve.Attributes;

public class JobQueryParameters : QueryParameters
{
    [Sieve(CanSort = true, CanFilter = true)]
    public int? Status { get; set; }

    [Sieve(CanSort = true, CanFilter = true)]
    public string? JobType { get; set; }

    [Sieve(CanSort = true, CanFilter = true)]
    public string? Title { get; set; }

    [Sieve(CanSort = true, CanFilter = true)]
    public string? Description { get; set; }

    [Sieve(CanSort = true, CanFilter = true)]
    public DateTimeOffset? CreatedAt { get; set; }
}
}