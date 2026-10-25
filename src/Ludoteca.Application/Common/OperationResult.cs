namespace Ludoteca.Application.Common;

public class OperationResult
{
    protected OperationResult(bool sucesso, Error? erro)
    {
        IsSuccess = sucesso;
        Error = erro;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static OperationResult Success() => new(true, null);

    public static OperationResult Failure(Error erro) => new(false, erro);
}

public class OperationResult<T> : OperationResult
{
    private OperationResult(T? dados, bool sucesso, Error? erro)
        : base(sucesso, erro)
    {
        Data = dados;
    }

    public T? Data { get; }

    public static OperationResult<T> Success(T dados) => new(dados, true, null);

    public new static OperationResult<T> Failure(Error erro) => new(default, false, erro);
}
