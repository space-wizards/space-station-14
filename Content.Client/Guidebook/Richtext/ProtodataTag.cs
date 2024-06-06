using System.Globalization;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.Guidebook.RichText;

/// <summary>
/// RichText tag that can display values extracted from entity prototypes.
/// In order to be accessed by this tag, the desired field/property must
/// be tagged with <see cref="Shared.Guidebook.GuidebookDataAttribute"/>.
/// </summary>
public sealed class ProtodataTag : IMarkupTag
{
    [Dependency] private readonly ILogManager _logMan = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    public string Name => "protodata";
    private ISawmill Log => _log ??= _logMan.GetSawmill("protodata_tag");
    private ISawmill? _log;

    public string TextBefore(MarkupNode node)
    {
        // Do nothing with an empty tag
        if (!node.Value.TryGetString(out var prototype))
            return string.Empty;

        if (!node.Attributes.TryGetValue("comp", out var component))
            return string.Empty;
        if (!node.Attributes.TryGetValue("member", out var member))
            return string.Empty;
        node.Attributes.TryGetValue("format", out var format);

        var guidebookData = _entMan.System<GuidebookDataSystem>();

        // Try to get the value
        if (!guidebookData.TryGetValue(prototype, component.StringValue!, member.StringValue!, out var value))
        {
            Log.Error($"Failed to find protodata for {component}.{member} in {prototype}");
            return "???";
        }

        // If we have a format string and a formattable value, format it as requested
        if (!string.IsNullOrEmpty(format.StringValue) && value is IFormattable formattable)
            return formattable.ToString(format.StringValue, CultureInfo.CurrentCulture);

        // No format string given, so just use default ToString
        return value?.ToString() ?? "NULL";
    }
}
