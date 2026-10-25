namespace Ludoteca.Api.Hateoas;

public sealed class Resource<T>
{
    public Resource(T data, IReadOnlyList<Link> links)
    {
        Data = data;
        Links = links;
    }

    public T Data { get; }

    public IReadOnlyList<Link> Links { get; }
}
