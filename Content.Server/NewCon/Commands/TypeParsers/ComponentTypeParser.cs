using System.Diagnostics.CodeAnalysis;

namespace Content.Server.NewCon.Commands.TypeParsers;

public sealed class ComponentTypeParser : TypeParser<ComponentType>
{
    [Dependency] private readonly IComponentFactory _factory = default!;

    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result)
    {
        var word = parser.GetWord();

        if (word is null)
        {
            result = null;
            return false;
        }

        if (!_factory.TryGetRegistration(word, out var reg, true))
        {
            result = null;
            return false;
        }

        result = new ComponentType(reg.Type);
        return true;
    }
}

public readonly record struct ComponentType(Type Ty);
