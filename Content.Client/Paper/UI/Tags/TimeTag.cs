using Content.Shared.Paper;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.Paper.UI.Tags;

public sealed class TimeTag : IMarkupTag
{
    public string Name => "time";

    public string TextBefore(MarkupNode node)
    {
        var defaultStr = "00:00:00";
        if (!node.Attributes.TryGetValue("state", out var compound))
            return defaultStr;
        var time = (compound.CompoundValue as SharedPaperComponent.TagsState)!.WriteTime;
        return time.HasValue ? time.Value.ToString(@"hh\:mm\:ss") : defaultStr;
    }
}
