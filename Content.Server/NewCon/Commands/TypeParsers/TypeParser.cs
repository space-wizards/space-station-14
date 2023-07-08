using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Content.Server.NewCon.Commands.TypeParsers;

public interface ITypeParser
{
    public Type Parses { get; }

    public bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result);
}

public abstract class TypeParser<T> : ITypeParser
    where T: notnull
{
    public Type Parses => typeof(T);

    public abstract bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result);
}
