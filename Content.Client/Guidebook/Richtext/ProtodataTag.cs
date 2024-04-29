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
    public string Name => "protodata";

    public string TextBefore(MarkupNode node)
    {
        // Do nothing with an empty tag
        if (!node.Value.TryGetString(out var command))
            return "";

        var guidebookData = IoCManager.Resolve<IEntityManager>().System<GuidebookDataSystem>();

        // Split the ID and the format string
        var parts = command.Split(':');
        DebugTools.Assert(parts.Length > 0 && parts.Length < 3, "Incorrect protodata format. Use Prototype.Component.Field or Prototype.Component.Field:Format");

        // Split the ID into Prototype, Component, and Field
        var idParts = parts[0].Split('.');
        DebugTools.Assert(idParts.Length == 3, "Incorrect protodata format. Use Prototype.Component.Field or Prototype.Component.Field:Format");

        // Try to get the value
        if (!guidebookData.TryGetValue(idParts[0], idParts[1], idParts[2], out var value))
            return "";

        // If we have a format string and a formattable value, format it as requested
        if (parts.Length > 1 && value is IFormattable formattable)
            return formattable.ToString(parts[1], CultureInfo.CurrentCulture);

        // No format string given, so just use default ToString
        return value?.ToString() ?? "NULL";
    }
}
