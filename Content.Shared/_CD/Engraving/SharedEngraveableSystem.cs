using Content.Shared.Examine;
using Robust.Shared.Utility;

namespace Content.Shared._CD.Engraving;

public abstract class SharedEngraveableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EngraveableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<EngraveableComponent> ent, ref ExaminedEvent args)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString(ent.Comp.EngravedMessage == string.Empty
            ? ent.Comp.NoEngravingText
            : ent.Comp.HasEngravingText));

        if (ent.Comp.EngravedMessage != string.Empty)
            msg.AddMarkupPermissive(Loc.GetString(ent.Comp.EngravedMessage));

        args.PushMessage(msg, 1);
    }
}
