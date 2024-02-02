using Content.Shared.Paper;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.Paper.UI.Tags;

public sealed class TimeTag : IMarkupTag
{
    public string Name => "time";

    public string TextBefore(MarkupNode node)
    {
        if (node.Attributes.TryGetValue("state", out var compound))
        {
            var time = (compound.CompoundValue as SharedPaperComponent.TagsState)!.WriteTime!.Value;
            return time.ToString(@"hh\:mm\:ss");
        }

        return "00:00:00";
    }
}
