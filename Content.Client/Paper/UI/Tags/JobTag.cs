using Content.Shared.Paper;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.Paper.UI.Tags;

public sealed class JobTag : IMarkupTag
{
    public string Name => "job";

    public string TextBefore(MarkupNode node)
    {
        var defaultStr = Loc.GetString("paper-tags-person-job-default");
        if (!node.Attributes.TryGetValue("state", out var compound))
            return defaultStr;
        var jobName = (compound.CompoundValue as SharedPaperComponent.TagsState)!.PersonJob;
        return jobName ?? defaultStr;
    }
}
