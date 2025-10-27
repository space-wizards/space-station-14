using Content.Shared.Atmos.Components;
using Content.Shared.Examine;

namespace Content.Shared.Atmos.EntitySystems;

/// <summary>
/// System for handling shared DeltaPressureSystem logic like predicted examine.
/// </summary>
public abstract partial class SharedDeltaPressureSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeltaPressureComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<DeltaPressureComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.IsTakingDamage)
            args.PushMarkup(Loc.GetString("window-taking-damage"));

    }
}
