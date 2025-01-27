using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.DetailedInspect;

public sealed partial class DetailedInspectSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DetailedInspectComponent, GetVerbsEvent<ExamineVerb>>(OnGetVerb);
    }

    public void OnGetVerb(Entity<DetailedInspectComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !_net.IsServer)
            return;

        var msg = new FormattedMessage();
        var numberedIndex = 1;

        foreach (var locId in ent.Comp.ExamineText)
        {
            if (ent.Comp.TickEntries)
                msg.AddMarkupOrThrow("- ");

            if (ent.Comp.NumberedEntries)
            {
                msg.AddMarkupOrThrow($"{numberedIndex}. ");
                numberedIndex++;
            }

            msg.AddMarkupOrThrow(Loc.GetString(locId));

            if (ent.Comp.LineBreak)
                msg.PushNewline();
            else
                msg.AddMarkupOrThrow(" ");
        }


        _examine.AddDetailedExamineVerb(args, ent.Comp, msg, Loc.GetString(ent.Comp.VerbText), ent.Comp.Icon, Loc.GetString(ent.Comp.VerbMessage));
    }
}
