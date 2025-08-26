using Content.Shared.Random.Helpers;
using Content.Shared.Trigger.Components.Conditions;
using Content.Shared.Verbs;
using Robust.Shared.Random;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    private void InitializeCondition()
    {
        SubscribeLocalEvent<WhitelistTriggerConditionComponent, AttemptTriggerEvent>(OnWhitelistTriggerAttempt);

        SubscribeLocalEvent<UseDelayTriggerConditionComponent, AttemptTriggerEvent>(OnUseDelayTriggerAttempt);

        SubscribeLocalEvent<ToggleTriggerConditionComponent, AttemptTriggerEvent>(OnToggleTriggerAttempt);
        SubscribeLocalEvent<ToggleTriggerConditionComponent, GetVerbsEvent<AlternativeVerb>>(OnToggleGetAltVerbs);

        SubscribeLocalEvent<RandomChanceTriggerConditionComponent, AttemptTriggerEvent>(OnRandomChanceTriggerAttempt);
    }

    /// <summary>
    /// Method placed at the start of trigger condition subscriptions to evaluate if this condition cares about this attempt event.
    /// </summary>
    /// <returns>True if this condition should try to modify this event.</returns>
    private static bool ConditionPrefix<TComp>(TComp comp, ref AttemptTriggerEvent ev) where TComp : BaseTriggerConditionComponent
    {
        // If the trigger is already cancelled we only need to run checks if this condition wants to add a cancel trigger
        if (ev.Cancelled && comp.CancelKeyOut == null)
            return false;

        // Check if this condition cares about the trigger attempt
        return ev.Key == null || comp.Keys.Contains(ev.Key);
    }

    /// <summary>
    /// Method placed at the end of trigger condition subscriptions to modify the event.
    /// </summary>
    /// <param name="result">What the condition evaluated to.</param>
    private static void ConditionSuffix<TComp>(TComp comp, bool result, ref AttemptTriggerEvent ev) where TComp : BaseTriggerConditionComponent
    {
        if (comp.Inverted)
            result = !result;

        // Only add the key to the cancel trigger if this condition (not another condition) would cancel it
        if (result && comp.CancelKeyOut != null)
            ev.CancelKeys.Add(comp.CancelKeyOut);

        // Bitwise operation to assign true to args.Cancelled if cancel is true,
        // but to also never overwrite with false if it was set to true somewhere else
        ev.Cancelled |= result;
    }

    private void OnWhitelistTriggerAttempt(Entity<WhitelistTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (!ConditionPrefix(ent.Comp, ref args))
            return;

        var cancel = !_whitelist.CheckBoth(args.User, ent.Comp.UserBlacklist, ent.Comp.UserWhitelist);

        ConditionSuffix(ent.Comp, cancel, ref args);
    }

    private void OnUseDelayTriggerAttempt(Entity<UseDelayTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (!ConditionPrefix(ent.Comp, ref args))
            return;

        var cancel = _useDelay.IsDelayed(ent.Owner, ent.Comp.UseDelayId);

        ConditionSuffix(ent.Comp, cancel, ref args);
    }

    private void OnToggleTriggerAttempt(Entity<ToggleTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (!ConditionPrefix(ent.Comp, ref args))
            return;

        var cancel = !ent.Comp.Enabled;

        ConditionSuffix(ent.Comp, cancel, ref args);
    }

    private void OnToggleGetAltVerbs(Entity<ToggleTriggerConditionComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString(ent.Comp.ToggleVerb),
            Act = () => Toggle(ent, user)
        });
    }

    private void Toggle(Entity<ToggleTriggerConditionComponent> ent, EntityUid user)
    {
        var msg = ent.Comp.Enabled ? ent.Comp.ToggleOff : ent.Comp.ToggleOn;
        _popup.PopupPredicted(Loc.GetString(msg), ent.Owner, user);
        ent.Comp.Enabled = !ent.Comp.Enabled;
        Dirty(ent);
    }

    private void OnRandomChanceTriggerAttempt(Entity<RandomChanceTriggerConditionComponent> ent,
        ref AttemptTriggerEvent args)
    {
        if (!ConditionPrefix(ent.Comp, ref args))
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var hash = new List<int>
        {
            (int)_timing.CurTick.Value,
            GetNetEntity(ent).Id,
            args.User == null ? 0 : GetNetEntity(args.User.Value).Id,
        };
        var seed = SharedRandomExtensions.HashCodeCombine(hash);
        var rand = new System.Random(seed);

        var cancel = !rand.Prob(ent.Comp.SuccessChance); // When not successful, cancel = true

        ConditionSuffix(ent.Comp, cancel, ref args);
    }
}
