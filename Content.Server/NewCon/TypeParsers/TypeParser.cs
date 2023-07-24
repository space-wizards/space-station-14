using System.Diagnostics.CodeAnalysis;
using Content.Server.NewCon.Errors;
using JetBrains.Annotations;

namespace Content.Server.NewCon.TypeParsers;

public interface ITypeParser
{
    public Type Parses { get; }

    public bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error);
}

[MeansImplicitUse]
public abstract class TypeParser<T> : ITypeParser
    where T: notnull
{
    public Type Parses => typeof(T);

    public abstract bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error);
}
