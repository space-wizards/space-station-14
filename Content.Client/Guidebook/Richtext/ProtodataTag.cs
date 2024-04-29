using System.Globalization;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.Guidebook.RichText;

public sealed class ProtodataTag : IMarkupTag
{
    public string Name => "protodata";

    public string TextBefore(MarkupNode node)
    {
        if (!node.Value.TryGetString(out var command))
            return "";

        var guidebookData = IoCManager.Resolve<IEntityManager>().System<GuidebookDataSystem>();

        var parts = command.Split(':');
        DebugTools.Assert(parts.Length > 0 && parts.Length < 3, "Incorrect protodata format. Use Prototype.Component.Field or Prototype.Component.Field:Format");
        var idParts = parts[0].Split('.');
        DebugTools.Assert(idParts.Length == 3, "Incorrect protodata format. Use Prototype.Component.Field or Prototype.Component.Field:Format");
        if (!guidebookData.TryGetValue(idParts[0], idParts[1], idParts[2], out var value))
            return "";

        if (parts.Length > 1 && value is IFormattable formattable)
            return formattable.ToString(parts[1], CultureInfo.CurrentCulture);

        return value.ToString() ?? "NULL";
    }
}
