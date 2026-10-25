namespace Ludoteca.Tests.Unit.Support;

internal sealed class RelogioFixo : TimeProvider
{
    private readonly DateTimeOffset _agora;

    public RelogioFixo(DateTime utc)
    {
        _agora = new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
    }

    public override DateTimeOffset GetUtcNow() => _agora;
}
