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
        SubscribeLocalEvent<TriggerOnCancelledTriggerComponent, CancelledTriggerEvent>(OnCancelledTrigger);

        SubscribeLocalEvent<WhitelistTriggerConditionComponent, AttemptTriggerEvent>(OnWhitelistTriggerAttempt);

        SubscribeLocalEvent<UseDelayTriggerConditionComponent, AttemptTriggerEvent>(OnUseDelayTriggerAttempt);

        SubscribeLocalEvent<ToggleTriggerConditionComponent, AttemptTriggerEvent>(OnToggleTriggerAttempt);
        SubscribeLocalEvent<ToggleTriggerConditionComponent, GetVerbsEvent<AlternativeVerb>>(OnToggleGetAltVerbs);

        SubscribeLocalEvent<RandomChanceTriggerConditionComponent, AttemptTriggerEvent>(OnRandomChanceTriggerAttempt);
    }

    private void OnCancelledTrigger(Entity<TriggerOnCancelledTriggerComponent> ent, ref CancelledTriggerEvent args)
    {
        // Ignored unlike other triggers, as it can recursively trigger a trigger guaranteed to fail
        if (args.Key == null)
            return;

        if (!ent.Comp.KeysInOut.TryGetValue(args.Key, out var keyOut))
            return;

        // Can easily create an infinite loop
        if (keyOut == args.Key)
            return;

        Trigger(ent.Owner, args.User, keyOut);
    }

    private void OnWhitelistTriggerAttempt(Entity<WhitelistTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key == null || ent.Comp.Keys.Contains(args.Key))
            args.Cancelled |= !_whitelist.CheckBoth(args.User, ent.Comp.UserBlacklist, ent.Comp.UserWhitelist);
    }

    private void OnUseDelayTriggerAttempt(Entity<UseDelayTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key == null || ent.Comp.Keys.Contains(args.Key))
            args.Cancelled |= _useDelay.IsDelayed(ent.Owner, ent.Comp.UseDelayId);
    }

    private void OnToggleTriggerAttempt(Entity<ToggleTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key == null || ent.Comp.Keys.Contains(args.Key))
            args.Cancelled |= !ent.Comp.Enabled;
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
        if (args.Key == null || ent.Comp.Keys.Contains(args.Key))
        {
            // TODO: Replace with RandomPredicted once the engine PR is merged
            var hash = new List<int>
            {
                (int)_timing.CurTick.Value,
                GetNetEntity(ent).Id,
                args.User == null ? 0 : GetNetEntity(args.User.Value).Id,
            };
            var seed = SharedRandomExtensions.HashCodeCombine(hash);
            var rand = new System.Random(seed);

            args.Cancelled |= !rand.Prob(ent.Comp.SuccessChance); // When not successful, Cancelled = true
        }
    }
}
