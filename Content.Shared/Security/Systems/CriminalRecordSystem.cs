using Content.Shared.Overlays;
using Content.Shared.Security.Components;

namespace Content.Shared.Security.Systems;

public sealed class CriminalRecordSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CriminalRecordComponent, GetCriminalIconEvent>(OnGetCriminalIconEvent);
    }

    /// <summary>
    ///     Gets the icon associated with the person's criminal status.
    /// </summary>
    private void OnGetCriminalIconEvent(Entity<CriminalRecordComponent> ent, ref GetCriminalIconEvent args)
    {
        args.Icon = ent.Comp.StatusIcon;
    }
}
