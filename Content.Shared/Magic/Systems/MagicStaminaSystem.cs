using Content.Shared.Damage.Systems;
using Content.Shared.Magic.Events;
using Content.Shared.Magic.Components;
using Content.Shared.Popups;

namespace Content.Shared.Magic.Systems;

/// <summary>
/// Handles consuming stamina for spells/actions that declare a stamina cost via
/// <see cref="ConsumeStaminaOnCastComponent"/>. Runs as part of the "before cast" checks
/// so casting can be cancelled if the performer lacks stamina.
/// </summary>
public sealed class MagicStaminaSystem : EntitySystem
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConsumeStaminaOnCastComponent, BeforeCastSpellEvent>(OnBeforeCast);
    }

    private void OnBeforeCast(Entity<ConsumeStaminaOnCastComponent> ent, ref BeforeCastSpellEvent args)
    {
        var comp = ent.Comp;
        if (comp.Amount <= 0f)
            return;

        // If performer has no stamina component, treat as success (no stamina to deduct).
        if (!TryComp<Content.Shared.Damage.Components.StaminaComponent>(args.Performer, out var staminaComp))
            return;

        var success = _stamina.TryTakeStamina(args.Performer, comp.Amount, component: staminaComp, source: ent.Owner);

        if (!success)
        {
            args.Cancelled = true;
            _popup.PopupClient("Not enough stamina.", args.Performer, args.Performer);
        }
    }
}
