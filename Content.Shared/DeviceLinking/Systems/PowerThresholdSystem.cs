using Content.Shared.Administration.Logs;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.DeviceLinking.Systems;

/// <summary>
/// Allows an entity to set power sensor threshold.
/// </summary>
public sealed class PowerThresholdSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    /// <summary>
    ///     Default threshold amounts for the set-threshold verb.
    /// </summary>
    public static readonly FixedPoint2[] DefaultThresholdAmounts = new FixedPoint2[] { 0, 5000, 10000, 25000, 50000, 100000, 250000, 500000, 1000000 };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerThresholdComponent, GetVerbsEvent<AlternativeVerb>>(AddSetThresholdVerbs);
        SubscribeLocalEvent<PowerThresholdComponent, ThresholdAmountSetValueMessage>(OnThresholdAmountSetValueMessage);
    }

    private void OnThresholdAmountSetValueMessage(Entity<PowerThresholdComponent> ent, ref ThresholdAmountSetValueMessage message)
    {
        var (uid, comp) = ent;

        var newThresholdAmount = FixedPoint2.Clamp(message.Value, comp.MinimumThresholdAmount, comp.MaximumThresholdAmount);
        comp.ThresholdAmount = newThresholdAmount;

        if (message.Actor is { Valid: true } user)
            _popup.PopupEntity(Loc.GetString("comp-power-threshold-set-amount", ("amount", newThresholdAmount)), uid, user);

        Dirty(uid, comp);
    }

    private void AddSetThresholdVerbs(Entity<PowerThresholdComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var (uid, comp) = ent;

        if (!args.CanAccess || !args.CanInteract || !comp.CanChangeThresholdAmount || args.Hands == null)
            return;

        // Custom threshold verb
        var @event = args;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("comp-power-threshold-verb-custom-amount"),
            Category = VerbCategory.SetPowerThreshold,
            // TODO: remove server check when bui prediction is a thing
            Act = () =>
            {
                _ui.OpenUi(uid, ThresholdAmountUiKey.Key, @event.User);
            },
            Priority = 1
        });

        // Add default threshold verbs
        var priority = 0;
        var user = args.User;
        foreach (var amount in DefaultThresholdAmounts)
        {
            AlternativeVerb verb = new();
            verb.Text = Loc.GetString("comp-power-threshold-verb-amount", ("amount", amount));
            verb.Category = VerbCategory.SetPowerThreshold;
            verb.Act = () =>
            {
                comp.ThresholdAmount = amount;

                _popup.PopupClient(Loc.GetString("comp-power-threshold-set-amount", ("amount", amount)), uid, user);

                Dirty(uid, comp);
            };

            // we want to sort by size, not alphabetically by the verb text.
            verb.Priority = priority;
            priority--;

            args.Verbs.Add(verb);
        }
    }
}
