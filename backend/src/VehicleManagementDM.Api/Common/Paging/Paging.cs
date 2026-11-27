using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace VehicleManagementDM.Api.Common.Paging;

/// <summary>Page parameters for list endpoints: <c>?page=1&amp;pageSize=20</c> (ARCHITECTURE.md §7).</summary>
public class PageQuery
{
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue, ErrorMessage = "Page must be 1 or more.")]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "Page size must be between 1 and 100.")]
    public int PageSize { get; init; } = 20;
}

/// <summary>One page of a list: <c>{ items, page, pageSize, totalCount }</c>.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public static class PagingExtensions
{
    /// <summary>Counts and fetches one page. The query must already be ordered.</summary>
    public static async Task<PagedResult<T>> ToPageAsync<T>(this IQueryable<T> query, PageQuery page, CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page.Page - 1) * page.PageSize).Take(page.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<T>(items, page.Page, page.PageSize, total);
    }
}
