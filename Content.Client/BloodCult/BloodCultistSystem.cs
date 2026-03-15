using Content.Shared.BloodCult;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.BloodCult;

public sealed class BloodCultistSystem : SharedBloodCultistSystem
{
	[Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultistComponent, GetStatusIconsEvent>(GetBloodCultistIcon);
    }

	private void GetBloodCultistIcon(Entity<BloodCultistComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.Resolve(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
