using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Content.Server.Paper;

namespace Content.Server.Labels.EntitySystems;

public sealed partial class LabelSystem : SharedLabelSystem
{
    [SubscribeLocalEvent]
    private void OnBountyCopied(Entity<LabelComponent> original, ref PaperCopiedEvent evt)
    {
        Label(evt.Copy, original.Comp.CurrentLabel);
    }
}
