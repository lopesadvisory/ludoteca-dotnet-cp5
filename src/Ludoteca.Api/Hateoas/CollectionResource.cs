using Ludoteca.Application.Common;

namespace Ludoteca.Api.Hateoas;

public sealed class PaginationInfo
{
    public PaginationInfo(int page, int pageSize, int itemCount, int totalRecords, int totalPages, bool hasPrevious, bool hasNext)
    {
        Page = page;
        PageSize = pageSize;
        ItemCount = itemCount;
        TotalRecords = totalRecords;
        TotalPages = totalPages;
        HasPrevious = hasPrevious;
        HasNext = hasNext;
    }

    public int Page { get; }

    public int PageSize { get; }

    public int ItemCount { get; }

    public int TotalRecords { get; }

    public int TotalPages { get; }

    public bool HasPrevious { get; }

    public bool HasNext { get; }

    public static PaginationInfo De<T>(PageResult<T> pagina)
    {
        return new PaginationInfo(
            pagina.Page,
            pagina.PageSize,
            pagina.ItemCount,
            pagina.TotalRecords,
            pagina.TotalPages,
            pagina.HasPrevious,
            pagina.HasNext);
    }
}

public sealed class CollectionResource<T>
{
    public CollectionResource(IReadOnlyList<Resource<T>> data, PaginationInfo pagination, IReadOnlyList<Link> links)
    {
        Data = data;
        Pagination = pagination;
        Links = links;
    }

    public IReadOnlyList<Resource<T>> Data { get; }

    public PaginationInfo Pagination { get; }

    public IReadOnlyList<Link> Links { get; }
}
