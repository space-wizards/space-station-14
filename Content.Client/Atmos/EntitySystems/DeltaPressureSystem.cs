using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Examine;

namespace Content.Client.Atmos.EntitySystems;

public sealed class DeltaPressureSystem : SharedDeltaPressureSystem
{
    protected override void OnExaminedEvent(Entity<DeltaPressureComponent> ent, ref ExaminedEvent args)
    {
        base.OnExaminedEvent(ent, ref args);

        if (ent.Comp.IsTakingDamage)
            args.PushMarkup(Loc.GetString("window-taking-damage"));
    }
}
