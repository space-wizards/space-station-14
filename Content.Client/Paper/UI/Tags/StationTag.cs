using Content.Shared.Paper;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.Paper.UI.Tags;

public sealed class StationTag : IMarkupTag
{
    public string Name => "station";

    public string TextBefore(MarkupNode node)
    {
        var defaultStr = Loc.GetString("paper-tags-station-name-default");
        if (!node.Attributes.TryGetValue("state", out var compound))
            return defaultStr;
        var stationName = (compound.CompoundValue as SharedPaperComponent.TagsState)!.StationName;
        return stationName ?? defaultStr;
    }
}
