using Content.Shared.Ninja.Components;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;

namespace Content.Shared.Ninja.Systems;

public abstract class SharedNinjaSystem : EntitySystem
{
    [Dependency] protected readonly SharedStealthSystem _stealth = default!;

    /// <summary>
    /// Bind a katana entity to a ninja, letting it be recalled and dash.
    /// </summary>
    public void BindKatana(SpaceNinjaComponent comp, EntityUid katana)
    {
        comp.Katana = katana;
    }

	// TODO: remove when objective stuff moved into objectives somehow
    public void DetonateSpiderCharge(SpaceNinjaComponent comp)
    {
    	comp.SpiderChargeDetonated = true;
    }

    protected void SetCloaked(EntityUid user, bool cloaked)
    {
        if (!TryComp<StealthComponent>(user, out var stealth))
            return;

        // slightly visible, but doesn't change when moving so it's ok
        var visibility = cloaked ? stealth.MinVisibility + 0.25f : stealth.MaxVisibility;
        _stealth.SetVisibility(user, visibility, stealth);
        _stealth.SetEnabled(user, cloaked, stealth);
    }
}
