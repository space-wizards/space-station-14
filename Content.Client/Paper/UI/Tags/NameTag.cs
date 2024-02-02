using Content.Shared.Paper;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.Paper.UI.Tags;

public sealed class NameTag : IMarkupTag
{
    public string Name => "name";

    public string TextBefore(MarkupNode node)
    {
        // TODO: Think of something better
        var defaultStr = Loc.GetString("paper-tags-person-name-default");
        if (!node.Attributes.TryGetValue("state", out var compound))
            return defaultStr;
        var name = (compound.CompoundValue as SharedPaperComponent.TagsState)!.PersonName;
        return name ?? defaultStr;
    }
}
