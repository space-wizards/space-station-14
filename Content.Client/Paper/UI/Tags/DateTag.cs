using Content.Shared.Paper;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.Paper.UI.Tags;

public sealed class DateTag : IMarkupTag
{
    public string Name => "date";

    public string TextBefore(MarkupNode node)
    {
        string format = "dd.MM.yyyy",
            defaultStr = DateTime.Now.ToString(format);

        if (!node.Attributes.TryGetValue("state", out var compound))
            return defaultStr;
        var date = (compound.CompoundValue as SharedPaperComponent.TagsState)!.WriteDate;
        return date.HasValue ? date.Value.ToString("dd.MM.yyyy") : defaultStr;
    }
}
