namespace Ludoteca.Application.Common;

public sealed class PageResult<T>
{
    public PageResult(IReadOnlyList<T> data, int page, int pageSize, int totalRecords)
    {
        Data = data;
        Page = page;
        PageSize = pageSize;
        TotalRecords = totalRecords;
    }

    public IReadOnlyList<T> Data { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int ItemCount => Data.Count;

    public int TotalRecords { get; }

    public int TotalPages => TotalRecords == 0 ? 0 : (int)Math.Ceiling(TotalRecords / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public PageResult<TDestino> Converter<TDestino>(Func<T, TDestino> conversor)
    {
        return new PageResult<TDestino>(Data.Select(conversor).ToList(), Page, PageSize, TotalRecords);
    }
}
