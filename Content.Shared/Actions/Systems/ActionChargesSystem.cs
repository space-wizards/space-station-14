using Content.Shared.Actions.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Actions.Systems;

public sealed class ActionChargesSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActionChargesComponent, ActionAttemptEvent>(OnChargesAttempt);
        SubscribeLocalEvent<ActionChargesComponent, MapInitEvent>(OnChargesMapInit);
        SubscribeLocalEvent<ActionChargesComponent, ActionPerformedEvent>(OnChargesPerformed);
    }

    private void OnChargesAttempt(Entity<ActionChargesComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var charges = GetCurrentCharges((ent.Owner, ent.Comp, null));

        if (charges <= 0)
        {
            args.Cancelled = true;
        }
    }

    private void OnChargesPerformed(Entity<ActionChargesComponent> ent, ref ActionPerformedEvent args)
    {
        AddCharges((ent.Owner, ent.Comp), -1);
    }

    private void OnChargesMapInit(Entity<ActionChargesComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.LastCharges = ent.Comp.MaxCharges;
        ent.Comp.LastUpdate = _timing.CurTime;
        Dirty(ent);
    }

    public void AddCharges(Entity<ActionChargesComponent?> action, int removeCharges)
    {
        if (removeCharges == 0 || !Resolve(action.Owner, ref action.Comp, false))
            return;

        var oldCharges = GetCurrentCharges((action.Owner, action.Comp, null));
        var charges = Math.Clamp(oldCharges - removeCharges, 0, action.Comp.MaxCharges);

        if (oldCharges == charges)
            return;

        action.Comp.LastCharges = charges;
        action.Comp.LastUpdate = _timing.CurTime;
        Dirty(action);
    }

    /// <summary>
    /// Resets action charges to MaxCharges.
    /// </summary>
    public void ResetCharges(Entity<ActionChargesComponent?> action)
    {
        if (!Resolve(action.Owner, ref action.Comp, false))
            return;

        var charges = GetCurrentCharges((action.Owner, action.Comp, null));

        if (charges == action.Comp.MaxCharges)
            return;

        action.Comp.LastCharges = action.Comp.MaxCharges;
        action.Comp.LastUpdate = _timing.CurTime;
        Dirty(action);
    }

    public void SetCharges(Entity<ActionChargesComponent?> action, int value)
    {
        if (!Resolve(action.Owner, ref action.Comp, false))
            return;

        var adjusted = Math.Clamp(value, 0, action.Comp.MaxCharges);

        if (action.Comp.LastCharges == adjusted)
        {
            return;
        }

        action.Comp.LastCharges = adjusted;
        action.Comp.LastUpdate = _timing.CurTime;
        Dirty(action);
    }

    public int GetCurrentCharges(Entity<ActionChargesComponent?, ResetActionChargesComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp1, false))
        {
            return 0;
        }

        float updateRate = 0f;

        // If charges can reset then extrapolate how many charges we currently have.
        if (Resolve(entity.Owner, ref entity.Comp2, false) && _actions.TryGetActionData(entity.Owner, out var result))
        {
            updateRate = (float) (result.UseDelay?.TotalSeconds ?? 0f);
        }

        return Math.Clamp(entity.Comp1.LastCharges + (int) ((_timing.CurTime - entity.Comp1.LastUpdate).TotalSeconds * updateRate),
            0,
            entity.Comp1.MaxCharges);
    }
}
