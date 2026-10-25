namespace Ludoteca.Infrastructure.External;

public class ViaCepOptions
{
    public const string Secao = "ViaCep";

    public string UrlBase { get; set; } = "https://viacep.com.br/";

    public int TentativasDeRetry { get; set; } = 3;

    public int AtrasoEntreTentativasMilissegundos { get; set; } = 500;

    public int TimeoutPorTentativaSegundos { get; set; } = 5;

    public int TimeoutTotalSegundos { get; set; } = 20;

    public int CacheEmHoras { get; set; } = 24;
}
