using Content.Shared.StatusIcon.Components;
using Content.Shared.BloodBrother.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.BloodBrother;

public sealed class BloodBrotherSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedBloodBrotherComponent, GetStatusIconsEvent>(OnShowIcon);
    }

    private void OnShowIcon(Entity<SharedBloodBrotherComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
