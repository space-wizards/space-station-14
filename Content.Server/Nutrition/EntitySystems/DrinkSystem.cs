using Content.Server.Body.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Forensics;
using Content.Server.Inventory;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Nutrition.EntitySystems;

public sealed class DrinkSystem : SharedDrinkSystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly FoodSystem _food = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly ReactiveSystem _reaction = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly ForensicsSystem _forensics = default!;

    public override void Initialize()
    {
        base.Initialize();

        // TODO add InteractNoHandEvent for entities like mice.
        SubscribeLocalEvent<DrinkComponent, SolutionContainerChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<DrinkComponent, ComponentInit>(OnDrinkInit);
        // run before inventory so for bucket it always tries to drink before equipping (when empty)
        // run after openable so its always open -> drink
        SubscribeLocalEvent<DrinkComponent, UseInHandEvent>(OnUse, before: [typeof(ServerInventorySystem)], after: [typeof(OpenableSystem)]);
        SubscribeLocalEvent<DrinkComponent, AfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<DrinkComponent, ConsumeDoAfterEvent>(OnDoAfter);
    }

    /// <summary>
    /// Get the total hydration factor contained in a drink's solution.
    /// </summary>
    public float TotalHydration(EntityUid uid, DrinkComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return 0f;

        if (!_solutionContainer.TryGetSolution(uid, comp.Solution, out _, out var solution))
            return 0f;

        var total = 0f;
        foreach (var quantity in solution.Contents)
        {
            var reagent = _proto.Index<ReagentPrototype>(quantity.Reagent.Prototype);
            if (reagent.Metabolisms == null)
                continue;

            foreach (var entry in reagent.Metabolisms.Values)
            {
                foreach (var effect in entry.Effects)
                {
                    // ignores any effect conditions, just cares about how much it can hydrate
                    if (effect is SatiateThirst thirst)
                    {
                        total += thirst.HydrationFactor * quantity.Quantity.Float();
                    }
                }
            }
        }

        return total;
    }

    private void AfterInteract(Entity<DrinkComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        args.Handled = TryDrink(args.User, args.Target.Value, entity.Comp, entity);
    }

    private void OnUse(Entity<DrinkComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryDrink(args.User, args.User, entity.Comp, entity);
    }

    private void OnDrinkInit(Entity<DrinkComponent> entity, ref ComponentInit args)
    {
        if (TryComp<DrainableSolutionComponent>(entity, out var existingDrainable))
        {
            // Beakers have Drink component but they should use the existing Drainable
            entity.Comp.Solution = existingDrainable.Solution;
        }
        else
        {
            _solutionContainer.EnsureSolution(entity.Owner, entity.Comp.Solution, out _);
        }

        UpdateAppearance(entity, entity.Comp);

        if (TryComp(entity, out RefillableSolutionComponent? refillComp))
            refillComp.Solution = entity.Comp.Solution;

        if (TryComp(entity, out DrainableSolutionComponent? drainComp))
            drainComp.Solution = entity.Comp.Solution;
    }

    private void OnSolutionChange(Entity<DrinkComponent> entity, ref SolutionContainerChangedEvent args)
    {
        UpdateAppearance(entity, entity.Comp);
    }

    public void UpdateAppearance(EntityUid uid, DrinkComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
            !HasComp<SolutionContainerManagerComponent>(uid))
        {
            return;
        }

        var drainAvailable = DrinkVolume(uid, component);
        _appearance.SetData(uid, FoodVisuals.Visual, drainAvailable.Float(), appearance);
    }

    /// <summary>
    ///     Raised directed at a victim when someone has force fed them a drink.
    /// </summary>
    private void OnDoAfter(Entity<DrinkComponent> entity, ref ConsumeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || entity.Comp.Deleted)
            return;

        if (!TryComp<BodyComponent>(args.Target, out var body))
            return;

        if (args.Used is null || !_solutionContainer.TryGetSolution(args.Used.Value, args.Solution, out var soln, out var solution))
            return;

        if (_openable.IsClosed(args.Used.Value, args.Target.Value, predicted: true))
            return;

        // TODO this should really be checked every tick.
        if (_food.IsMouthBlocked(args.Target.Value))
            return;

        // TODO this should really be checked every tick.
        if (!_interaction.InRangeUnobstructed(args.User, args.Target.Value))
            return;

        var transferAmount = FixedPoint2.Min(entity.Comp.TransferAmount, solution.Volume);
        var drained = _solutionContainer.SplitSolution(soln.Value, transferAmount);
        var forceDrink = args.User != args.Target;

        args.Handled = true;
        if (transferAmount <= 0)
            return;

        if (!_body.TryGetBodyOrganEntityComps<StomachComponent>((args.Target.Value, body), out var stomachs))
        {
            _popup.PopupEntity(Loc.GetString(forceDrink ? "drink-component-try-use-drink-cannot-drink-other" : "drink-component-try-use-drink-had-enough"), args.Target.Value, args.User);

            if (HasComp<RefillableSolutionComponent>(args.Target.Value))
            {
                _puddle.TrySpillAt(args.User, drained, out _);
                return;
            }

            _solutionContainer.Refill(args.Target.Value, soln.Value, drained);
            return;
        }

        var firstStomach = stomachs.FirstOrNull(stomach => _stomach.CanTransferSolution(stomach.Owner, drained, stomach.Comp1));

        //All stomachs are full or can't handle whatever solution we have.
        if (firstStomach == null)
        {
            _popup.PopupEntity(Loc.GetString("drink-component-try-use-drink-had-enough"), args.Target.Value, args.Target.Value);

            if (forceDrink)
            {
                _popup.PopupEntity(Loc.GetString("drink-component-try-use-drink-had-enough-other"), args.Target.Value, args.User);
                _puddle.TrySpillAt(args.Target.Value, drained, out _);
            }
            else
                _solutionContainer.TryAddSolution(soln.Value, drained);

            return;
        }

        var flavors = args.FlavorMessage;

        if (forceDrink)
        {
            var targetName = Identity.Entity(args.Target.Value, EntityManager);
            var userName = Identity.Entity(args.User, EntityManager);

            _popup.PopupEntity(Loc.GetString("drink-component-force-feed-success", ("user", userName), ("flavors", flavors)), args.Target.Value, args.Target.Value);

            _popup.PopupEntity(
                Loc.GetString("drink-component-force-feed-success-user", ("target", targetName)),
                args.User, args.User);

            // log successful forced drinking
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(entity.Owner):user} forced {ToPrettyString(args.User):target} to drink {ToPrettyString(entity.Owner):drink}");
        }
        else
        {
            _popup.PopupEntity(
                Loc.GetString("drink-component-try-use-drink-success-slurp-taste", ("flavors", flavors)), args.User,
                args.User);
            _popup.PopupEntity(
                Loc.GetString("drink-component-try-use-drink-success-slurp"), args.User, Filter.PvsExcept(args.User), true);

            // log successful voluntary drinking
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.User):target} drank {ToPrettyString(entity.Owner):drink}");
        }

        _audio.PlayPvs(entity.Comp.UseSound, args.Target.Value, AudioParams.Default.WithVolume(-2f).WithVariation(0.25f));

        _reaction.DoEntityReaction(args.Target.Value, solution, ReactionMethod.Ingestion);
        _stomach.TryTransferSolution(firstStomach.Value.Owner, drained, firstStomach.Value.Comp1);

        _forensics.TransferDna(entity, args.Target.Value);

        if (!forceDrink && solution.Volume > 0)
            args.Repeat = true;
    }
}
