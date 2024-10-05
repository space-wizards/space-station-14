using Content.Shared.Examine;
using Content.Shared.Revenant.Components;

namespace Content.Shared.Revenant.EntitySystems;

public sealed partial class HauntedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HauntedComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, HauntedComponent comp, ExaminedEvent args)
    {
        if (HasComp<RevenantComponent>(args.Examiner))
        {
            args.PushMarkup($"[color=mediumpurple]{Loc.GetString("revenant-already-haunted", ("target", uid))}[/color]");
        }
    }
}
