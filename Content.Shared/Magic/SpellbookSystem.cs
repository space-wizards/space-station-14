using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Magic.Components;
using Content.Shared.Mind;
using Robust.Shared.Network;

namespace Content.Shared.Magic;

public sealed class SpellbookSystem : EntitySystem
{
    [Dependency] private readonly SharedChargesSystem _sharedCharges = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpellbookComponent, MapInitEvent>(OnInit, before: [typeof(SharedMagicSystem)]);
        SubscribeLocalEvent<SpellbookComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<SpellbookComponent, SpellbookDoAfterEvent>(OnDoAfter);
    }

    private void OnInit(Entity<SpellbookComponent> ent, ref MapInitEvent args)
    {
        foreach (var (id, charges) in ent.Comp.SpellActions)
        {
            var action = _actionContainer.AddAction(ent, id);
            if (action is not { } spell)
                continue;

            // Null means infinite charges.
            if (charges is { } count)
            {
                EnsureComp<LimitedChargesComponent>(spell, out var chargeComp);
                _sharedCharges.SetMaxCharges((spell, chargeComp), count);
                _sharedCharges.SetCharges((spell, chargeComp), count);
            }

            ent.Comp.Spells.Add(spell);
        }
    }

    private void OnUse(Entity<SpellbookComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        AttemptLearn(ent, args);

        args.Handled = true;
    }

    private void OnDoAfter<T>(Entity<SpellbookComponent> ent, ref T args) where T : DoAfterEvent // Sometimes i despise this language
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        if (!ent.Comp.LearnPermanently)
        {
            _actions.GrantActions(args.Args.User, ent.Comp.Spells, ent.Owner);
            return;
        }

        if (_mind.TryGetMind(args.Args.User, out var mindId, out _))
        {
            var mindActionContainerComp = EnsureComp<ActionsContainerComponent>(mindId);

            if (_netManager.IsServer)
                _actionContainer.TransferAllActionsWithNewAttached(ent, mindId, args.Args.User, newContainer: mindActionContainerComp);
        }
        else
        {
            foreach (var (id, charges) in ent.Comp.SpellActions)
            {
                EntityUid? actionId = null;
                if (!_actions.AddAction(args.Args.User, ref actionId, id)
                    || charges is not { } count // Null means infinite charges
                    || !TryComp<LimitedChargesComponent>(actionId, out var chargeComp))
                    continue;

                _sharedCharges.SetMaxCharges((actionId.Value, chargeComp), count);
                _sharedCharges.SetCharges((actionId.Value, chargeComp), count);
            }
        }

        ent.Comp.SpellActions.Clear();
    }

    private void AttemptLearn(Entity<SpellbookComponent> ent, UseInHandEvent args)
    {
        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.LearnTime, new SpellbookDoAfterEvent(), ent, target: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true, //What, are you going to read with your eyes only??
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }
}
