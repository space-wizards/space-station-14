using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Shared.Chemistry.EntitySystems;

public abstract class SharedInjectorSystem : EntitySystem
{
    /// <summary>
    ///     Default transfer amounts for the set-transfer verb.
    /// </summary>
    public static readonly FixedPoint2[] TransferAmounts = { 1, 5, 10, 15 };

    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem SolutionContainers = default!;
    [Dependency] protected readonly MobStateSystem MobState = default!;
    [Dependency] protected readonly SharedCombatModeSystem Combat = default!;
    [Dependency] protected readonly SharedDoAfterSystem DoAfter = default!;
    [Dependency] protected readonly ISharedAdminLogManager AdminLogger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InjectorComponent, GetVerbsEvent<AlternativeVerb>>(AddSetTransferVerbs);
        SubscribeLocalEvent<InjectorComponent, ComponentStartup>(OnInjectorStartup);
        SubscribeLocalEvent<InjectorComponent, UseInHandEvent>(OnInjectorUse);
    }

    private void AddSetTransferVerbs(Entity<InjectorComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (!HasComp<ActorComponent>(args.User))
            return;

        var (_, component) = entity;

        // Add specific transfer verbs according to the container's size
        var priority = 0;
        var user = args.User;
        foreach (var amount in TransferAmounts)
        {
            if (amount < component.MinimumTransferAmount || amount > component.MaximumTransferAmount)
                continue;

            AlternativeVerb verb = new()
            {
                Text = Loc.GetString("comp-solution-transfer-verb-amount", ("amount", amount)),
                Category = VerbCategory.SetTransferAmount,
                Act = () =>
                {
                    component.TransferAmount = amount;
                    Popup.PopupClient(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)), user, user);
                    Dirty(entity);
                },

                // we want to sort by size, not alphabetically by the verb text.
                Priority = priority
            };

            priority -= 1;

            args.Verbs.Add(verb);
        }
    }

    private void OnInjectorStartup(Entity<InjectorComponent> entity, ref ComponentStartup args)
    {
        // ???? why ?????
        Dirty(entity);
    }

    private void OnInjectorUse(Entity<InjectorComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        Toggle(entity, args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Toggle between draw/inject state if applicable
    /// </summary>
    private void Toggle(Entity<InjectorComponent> injector, EntityUid user)
    {
        if (injector.Comp.InjectOnly)
            return;

        string msg;
        switch (injector.Comp.ToggleState)
        {
            case InjectorToggleMode.Inject:
                SetMode(injector, InjectorToggleMode.Draw);
                msg = "injector-component-drawing-text";
                break;
            case InjectorToggleMode.Draw:
                SetMode(injector, InjectorToggleMode.Inject);
                msg = "injector-component-injecting-text";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        Popup.PopupClient(Loc.GetString(msg), injector, user);
    }

    public void SetMode(Entity<InjectorComponent> injector, InjectorToggleMode mode)
    {
        injector.Comp.ToggleState = mode;
        Dirty(injector);
    }
}
