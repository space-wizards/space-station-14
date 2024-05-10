using Content.Shared.Examine;
using Content.Shared.Labels.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Labels.EntitySystems;

public abstract partial class SharedLabelSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LabelComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, LabelComponent? label, ExaminedEvent args)
    {
        if (!Resolve(uid, ref label))
            return;

        if (label.CurrentLabel == null)
            return;

        var message = new FormattedMessage();
        message.AddText(Loc.GetString("hand-labeler-has-label", ("label", label.CurrentLabel)));
        args.PushMessage(message);
    }
}
