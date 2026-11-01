using Content.Server.Damage.Components;
using Content.Server.Popups;
using Content.Shared.Damage.Systems;
using Robust.Shared.Random;

namespace Content.Server.Damage.Systems;

/// <summary>
/// Outputs a random pop-up from the strings list when an object receives damage
/// </summary>
public sealed partial class DamageRandomPopupSystem : EntitySystem
{
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private IRobustRandom _random = default!;

    [SubscribeLocalEvent]
    private void OnDamageChange(Entity<DamageRandomPopupComponent> ent, ref DamageDealtEvent args)
    {
        if (args.AnyPositive)
            _popupSystem.PopupEntity(Loc.GetString(_random.Pick(ent.Comp.Popups)), ent);
    }
}
