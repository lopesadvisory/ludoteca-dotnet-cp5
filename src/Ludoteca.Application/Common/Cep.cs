namespace Ludoteca.Application.Common;

public static class Cep
{
    public static string? Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        var digitos = new string(valor.Where(char.IsDigit).ToArray());
        return digitos.Length == 8 ? digitos : null;
    }
}
