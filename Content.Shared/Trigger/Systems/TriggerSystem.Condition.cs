using Content.Shared.Random.Helpers;
using Content.Shared.Trigger.Components.Conditions;
using Content.Shared.Trigger.Components.Triggers;
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

    // Standard implementation of a trigger condition
    private void OnWhitelistTriggerAttempt(Entity<WhitelistTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        // If the trigger is already cancelled we only need to run checks if this condition wants to add a cancel trigger
        if (args.Cancelled && ent.Comp.CancelKeyOut == null)
            return;

        // Check if this condition cares about the trigger attempt
        if (args.Key != null && !ent.Comp.Keys.Contains(args.Key))
            return;

        // Unique condition check here!
        var cancel = !_whitelist.CheckBoth(args.User, ent.Comp.UserBlacklist, ent.Comp.UserWhitelist);

        // Only add the key to the cancel trigger if this condition (not another condition) would cancel it.
        if (cancel && ent.Comp.CancelKeyOut != null)
            args.CancelKeys.Add(ent.Comp.CancelKeyOut);

        // Bitwise operation to assign true to args.Cancelled if cancel is true,
        // but to also never overwrite an args.Cancelled that was set to true somewhere else
        if (ent.Comp.Inverted)
            args.Cancelled |= !cancel;
        else
            args.Cancelled |= cancel;
    }

    private void OnUseDelayTriggerAttempt(Entity<UseDelayTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Cancelled && ent.Comp.CancelKeyOut == null)
            return;

        if (args.Key != null && !ent.Comp.Keys.Contains(args.Key))
            return;

        var cancel = _useDelay.IsDelayed(ent.Owner, ent.Comp.UseDelayId);

        if (cancel && ent.Comp.CancelKeyOut != null)
            args.CancelKeys.Add(ent.Comp.CancelKeyOut);

        if (ent.Comp.Inverted)
            args.Cancelled |= !cancel;
        else
            args.Cancelled |= cancel;
    }

    private void OnToggleTriggerAttempt(Entity<ToggleTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Cancelled && ent.Comp.CancelKeyOut == null)
            return;

        if (args.Key != null && !ent.Comp.Keys.Contains(args.Key))
            return;

        var cancel = !ent.Comp.Enabled;

        if (cancel && ent.Comp.CancelKeyOut != null)
            args.CancelKeys.Add(ent.Comp.CancelKeyOut);

        if (ent.Comp.Inverted)
            args.Cancelled |= !cancel;
        else
            args.Cancelled |= cancel;
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
        if (args.Cancelled && ent.Comp.CancelKeyOut == null)
            return;

        if (args.Key != null && !ent.Comp.Keys.Contains(args.Key))
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

        if (cancel && ent.Comp.CancelKeyOut != null)
            args.CancelKeys.Add(ent.Comp.CancelKeyOut);

        if (ent.Comp.Inverted)
            args.Cancelled |= !cancel;
        else
            args.Cancelled |= cancel;
    }
}
