using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Content.Server.NewCon.Errors;
using Robust.Shared.Utility;

namespace Content.Server.NewCon.TypeParsers;

public sealed class ComponentTypeParser : TypeParser<ComponentType>
{
    [Dependency] private readonly IComponentFactory _factory = default!;

    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var start = parser.Index;
        var word = parser.GetWord();
        error = null;

        if (word is null)
        {
            error = new OutOfInputError();
            result = null;
            return false;
        }

        if (!_factory.TryGetRegistration(word.ToLower(), out var reg, true))
        {
            result = null;
            error = new UnknownComponentError(word);
            error.Contextualize(parser.Input, (start, parser.Index));
            return false;
        }

        result = new ComponentType(reg.Type);
        return true;
    }
}

public readonly record struct ComponentType(Type Ty) : IAsType<Type>
{
    public Type AsType() => Ty;
};

public record struct UnknownComponentError(string Component) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = FormattedMessage.FromMarkup(
            $"Unknown component {Component}. For a list of all components, try types:components."
            );
        if (Component.EndsWith("component", true, CultureInfo.InvariantCulture))
        {
            msg.PushNewline();
            msg.AddText($"Do not specify the word `Component` in the argument. Maybe try {Component[..^"component".Length]}?");
        }

        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
