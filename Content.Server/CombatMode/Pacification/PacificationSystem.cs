using Content.Server.Damage.Components;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Server.Fluids.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Player;

namespace Content.Server.CombatMode.Pacification;

public sealed class PacificationSystem : SharedPacificationSystem
{
    [Dependency] private   readonly SharedPopupSystem _popup = default!;
    [Dependency] private   readonly OpenableSystem _openable = default!;
    [Dependency] private   readonly SolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PacifiedComponent, BeforeThrowEvent>(OnBeforeThrow);
    }

    public void OnBeforeThrow(EntityUid uid, PacifiedComponent component, BeforeThrowEvent args)
    {
        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("pacified-cannot-throw"), args.PlayerUid, args.PlayerUid);
    }

    /// <summary>
    /// Returns true if the given entity is considered harmful when thrown.
    /// </summary>
    public bool IsEntityFragile(EntityUid uid, int fragileDamageThreshold)
    {
        // It damages entities on hit.
        if (HasComp<DamageOnHitComponent>(uid))
            return true;

        // It can be spilled easily and has something to spill.
        if (HasComp<SpillableComponent>(uid)
            && TryComp<OpenableComponent>(uid, out var openable)
            && !_openable.IsClosed(uid, null, openable)
            && _solutionContainerSystem.PercentFull(uid) > 0)
            return true;

        // It might be made of non-reinforced glass.
        if (TryComp(uid, out DamageableComponent? damageableComponent)
            && damageableComponent.DamageModifierSetId == "Glass")
            return true;

        return false;
    }
}
