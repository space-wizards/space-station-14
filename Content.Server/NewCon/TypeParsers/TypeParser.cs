using System.Diagnostics.CodeAnalysis;
using Content.Server.NewCon.Errors;
using JetBrains.Annotations;

namespace Content.Server.NewCon.TypeParsers;

public interface ITypeParser : IPostInjectInit
{
    public Type Parses { get; }

    public bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error);
}

[MeansImplicitUse]
public abstract class TypeParser<T> : ITypeParser
    where T: notnull
{
    [Dependency] private readonly ILogManager _log = default!;

    protected ISawmill _sawmill = default!;

    public Type Parses => typeof(T);

    public abstract bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error);

    public void PostInject()
    {
        Logger.Debug("awawasadfs");
        _sawmill = _log.GetSawmill(GetType().PrettyName());
    }
}
