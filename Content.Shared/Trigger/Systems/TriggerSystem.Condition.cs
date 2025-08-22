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

    private void OnWhitelistTriggerAttempt(Entity<WhitelistTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Cancelled && ent.Comp.CancelKeyOut == null)
            return;

        if (args.Key == null || ent.Comp.Keys.Contains(args.Key))
        {
            var cancel = !_whitelist.CheckBoth(args.User, ent.Comp.UserBlacklist, ent.Comp.UserWhitelist);

            args.Cancelled |= cancel;
            if (cancel && ent.Comp.CancelKeyOut != null) // Only add the key if this condition (not another condition) would cancel it.
                args.CancelKeys.Add(ent.Comp.CancelKeyOut);
        }
    }

    private void OnUseDelayTriggerAttempt(Entity<UseDelayTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Cancelled && ent.Comp.CancelKeyOut == null)
            return;

        if (args.Key == null || ent.Comp.Keys.Contains(args.Key))
        {
            var cancel = _useDelay.IsDelayed(ent.Owner, ent.Comp.UseDelayId);

            args.Cancelled |= cancel;
            if (cancel && ent.Comp.CancelKeyOut != null)
                args.CancelKeys.Add(ent.Comp.CancelKeyOut);
        }
    }

    private void OnToggleTriggerAttempt(Entity<ToggleTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Cancelled && ent.Comp.CancelKeyOut == null)
            return;

        if (args.Key == null || ent.Comp.Keys.Contains(args.Key))
        {
            var cancel = !ent.Comp.Enabled;

            args.Cancelled |= cancel;
            if (cancel && ent.Comp.CancelKeyOut != null)
                args.CancelKeys.Add(ent.Comp.CancelKeyOut);
        }
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

            var cancel = !rand.Prob(ent.Comp.SuccessChance); // When not successful, cancel = true

            args.Cancelled |= cancel;
            if (cancel && ent.Comp.CancelKeyOut != null)
                args.CancelKeys.Add(ent.Comp.CancelKeyOut);
        }
    }
}
