using System.Globalization;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.Guidebook.RichText;

/// <summary>
/// RichText tag that can display values extracted from entity prototypes.
/// In order to be accessed by this tag, the desired field/property must
/// be tagged with <see cref="Shared.Guidebook.GuidebookDataAttribute"/>.
/// </summary>
/// <remarks>
/// The tag should be formatted as Prototype.Component.Field[:Format], where
/// <list type="bullet">
/// <item>Prototype is the ID of the prototype.</item>
/// <item>Component is the name of the component the field/property belongs to.</item>
/// <item>Field is the name of the tagged field or property for display.</item>
/// <item>Format is an optional parameter passed to <see cref="IFormattable.ToString"/> to control how the value is formatted
/// (if the value is a Type that implements <see cref="IFormattable"/>)</item>
/// </list>
/// </remarks>
public sealed class ProtodataTag : IMarkupTag
{
    [Dependency] private readonly ILogManager _logMan = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    public string Name => "protodata";
    private ISawmill Log => _log ??= _logMan.GetSawmill("protodata_tag");
    private ISawmill? _log;

    private const string BadSyntaxMessage = "Bad syntax: \"{0}\". Use \"Prototype.Component.Field[:Format]\"";

    public string TextBefore(MarkupNode node)
    {
        // Do nothing with an empty tag
        if (!node.Value.TryGetString(out var command))
            return "";

        var guidebookData = _entMan.System<GuidebookDataSystem>();

        // Split the ID and the format string
        var parts = command.Split(':');
        if (parts.Length < 1 || parts.Length > 2)
        {
            Log.Error(BadSyntaxMessage, command);
            return "???";
        }
        var id = parts[0];
        var format = parts.TryGetValue(1, out var f) ? f : string.Empty;

        // Split the ID into Prototype, Component, and Field
        var idParts = id.Split('.');
        if (idParts.Length != 3)
        {
            Log.Error(BadSyntaxMessage, command);
            return "???";
        }
        var prototype = idParts[0];
        var component = idParts[1];
        var member = idParts[2];

        // Try to get the value
        if (!guidebookData.TryGetValue(prototype, component, member, out var value))
        {
            Log.Error($"Failed to find protodata for {component}.{member} in {prototype}");
            return "???";
        }

        // If we have a format string and a formattable value, format it as requested
        if (!string.IsNullOrEmpty(format) && value is IFormattable formattable)
            return formattable.ToString(parts[1], CultureInfo.CurrentCulture);

        // No format string given, so just use default ToString
        return value?.ToString() ?? "NULL";
    }
}
