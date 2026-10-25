namespace Ludoteca.Application.Common;

public sealed class PageRequest
{
    public const int TamanhoPadrao = 10;
    public const int TamanhoMaximo = 50;

    public PageRequest(int page, int pageSize)
    {
        Page = page < 1 ? 1 : page;
        PageSize = pageSize < 1 ? TamanhoPadrao : Math.Min(pageSize, TamanhoMaximo);
    }

    public int Page { get; }

    public int PageSize { get; }

    public int Skip => (Page - 1) * PageSize;
}
