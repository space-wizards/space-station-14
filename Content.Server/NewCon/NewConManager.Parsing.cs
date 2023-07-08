using System.Diagnostics.CodeAnalysis;
using Content.Server.NewCon.Commands.TypeParsers;

namespace Content.Server.NewCon;

public sealed partial class NewConManager
{
    private readonly Dictionary<Type, ITypeParser> _consoleTypeParsers = new();

    private void InitializeParser()
    {
        var parsers = _reflection.GetAllChildren<ITypeParser>();

        foreach (var parserType in parsers)
        {
            var parser = (ITypeParser)_typeFactory.CreateInstance(parserType);
            _consoleTypeParsers.Add(parser.Parses, parser);
        }
    }

    public ITypeParser? GetParserForType(Type t)
    {
        if (_consoleTypeParsers.TryGetValue(t, out var parser))
            return parser;

        var baseTy = t.BaseType;

        if (baseTy is not null)
            return GetParserForType(t);

        return null;
    }

    public bool TryParse(ForwardParser parser, Type t, [NotNullWhen(true)] out object? parsed)
    {
        var impl = GetParserForType(t);

        if (impl is null)
        {
            parsed = null;
            return false;
        }

        return impl.TryParse(parser, out parsed);
    }
}
