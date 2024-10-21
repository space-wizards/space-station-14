using Content.Shared.Examine;
using Content.Shared.Interaction.Events;

namespace Content.Client.Revenant;

public sealed partial class RevenantStasisSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantStasisComponent, ChangeDirectionAttemptEvent>(OnAttemptDirection);
        SubscribeLocalEvent<RevenantStasisComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<RevenantStasisComponent> entity, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("revenant-stasis-regenerating"));
    }

    private void OnAttemptDirection(EntityUid uid, RevenantStasisComponent comp, ChangeDirectionAttemptEvent args)
    {
        args.Cancel();
    }
}
