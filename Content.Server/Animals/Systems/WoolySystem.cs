using Content.Server.Animals.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Nutrition;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Wooly;
using Content.Shared.Verbs;
using Content.Shared.DoAfter;
using Content.Shared.Tools.Systems;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;
using Content.Server.Stack;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Server.Animals.Systems;

/// <summary>
///     Gives ability to produce fiber reagents, produces endless if the 
///     owner has no HungerComponent
/// </summary>
public sealed class WoolySystem : EntitySystem
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoolyComponent, BeforeFullyEatenEvent>(OnBeforeFullyEaten);
        SubscribeLocalEvent<WoolyComponent, GetVerbsEvent<AlternativeVerb>>(AddShearVerb);
        SubscribeLocalEvent<WoolyComponent, ShearingDoAfterEvent>(OnSheared);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<WoolyComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var wooly))
        {
            if (now < wooly.NextGrowth)
                continue;

            wooly.NextGrowth = now + wooly.GrowthDelay;

            if (_mobState.IsDead(uid))
                continue;

            // Actually there is food digestion so no problem with instant reagent generation "OnFeed"
            if (EntityManager.TryGetComponent(uid, out HungerComponent? hunger))
            {
                // Is there enough nutrition to produce reagent?
                if (_hunger.GetHungerThreshold(hunger) < HungerThreshold.Okay)
                    continue;

                _hunger.ModifyHunger(uid, -wooly.HungerUsage, hunger);
            }

            if (!_solutionContainer.ResolveSolution(uid, wooly.SolutionName, ref wooly.Solution))
                continue;

            _solutionContainer.TryAddReagent(wooly.Solution.Value, wooly.ReagentId, wooly.Quantity, out _);
        }
    }

    private void OnBeforeFullyEaten(Entity<WoolyComponent> ent, ref BeforeFullyEatenEvent args)
    {
        // don't want moths to delete goats after eating them
        args.Cancel();
    }

    /// <summary>
    ///     Attempts to shear the target animal, checking if it is wooly and building arguments for calling TryStartDoAfter.
    ///     Called by the "shear" verb.
    /// </summary>
    private void AttemptShear(Entity<WoolyComponent?> wooly, EntityUid userUid, EntityUid containerUid)
    {
        // Check the target creature has the wooly component.
        if (!Resolve(wooly, ref wooly.Comp))
            return;

        // Build arguments for calling TryStartDoAfter
        var doargs = new DoAfterArgs(
            EntityManager,
            userUid,
            5,
            new ShearingDoAfterEvent(),
            wooly,
            wooly,
            used: containerUid
        )
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1.0f,
        };

        // Triggers the ShearingDoAfter event.
        _doAfterSystem.TryStartDoAfter(doargs);
    }

    /// <summary>
    ///     Called by the ShearingDoAfter event.
    ///     Checks the action hasn't been cancelled, already handled, and that there's an item in the player's hand.
    ///     Checks that the target wooly creature contains a shearable solution.
    ///     Performs wool calcuations, then creates a corresponding pop-up message, and if successful spawns cotton under the wooly creature.
    /// </summary>
    private void OnSheared(Entity<WoolyComponent> ent, ref ShearingDoAfterEvent args)
    {
        // Check the action hasn't been cancelled, already handled, and that there's an item in the player's hand.
        if (args.Cancelled || args.Handled || args.Args.Used == null)
            return;

        // Resolves the wool as a solution inside the wooly creature. Outputs the "solution" variable.
        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return;

        // Mark as handled so we don't duplicate.
        args.Handled = true;

        // Store solution.Volume in a variable to make calculations a bit clearer.
        var woolQuantity = solution.Volume;

        // Failure message, if the wooly creature has no wool to be sheared.
        if (woolQuantity == 0)
        {
            _popup.PopupEntity(
                Loc.GetString(
                    "wooly-system-no-wool",
                    (
                        "target",
                        Identity.Entity(
                            ent.Owner,
                            EntityManager
                        )
                    )
                ),
                ent.Owner,
                args.Args.User
            );
            return;
        }

        // To moths 1 Wool is worth 3 Hunger; 1 Cotton is worth 15 Hunger.
        // 1 Wool is worth 100 FibreReagent. Wooly creatures store up to 25000 FibreReagent and start with 2500.
        // Therefore, we divide the FibreReagent by 500 to get the amount of Cotton to spawn, which has equal nutritional value.
        // Cotton is spawned because there is no wool item.
        var cottonPerWool = 500;

        // Modulas the woolQuantity so no wool is wasted if it can't be divided evenly.
        // At the same time, wooly creatures generate wool in batches of 25, so this scenario is actually impossible.
        // But, just in case...
        var woolToRemove = FixedPoint2.New(woolQuantity.Value - woolQuantity.Value % cottonPerWool);

        // Split the wool inside the wooly creatures by woolToRemove, return what was removed.
        var removedWool = _solutionContainer.SplitSolution(ent.Comp.Solution.Value, woolToRemove);

        // Target the wooly creature's location.
        var spawnCoordinates = Transform(ent).Coordinates;

        // Spawn cotton.
        _stackSystem.Spawn(removedWool.Volume.Value / cottonPerWool, "Cotton", spawnCoordinates);

        // Success message.
        _popup.PopupEntity(
            Loc.GetString(
                "wooly-system-success",
                (
                    "target",
                    Identity.Entity(
                        ent.Owner,
                        EntityManager
                    )
                )
            ),
            ent.Owner,
            args.Args.User,
            PopupType.Medium
        );
    }

    /// <summary>
    ///     Adds the "shear" verb to the player.
    ///     Checks first if the player is holding an item with the "cutting" component quality, such as wirecutters.
    /// </summary>
    private void AddShearVerb(Entity<WoolyComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {

        if (args.Using == null ||
             !args.CanInteract ||
             // Checks if you're using an item with the "cutting" component quality.
             !_tool.HasQuality(args.Using.Value, "Cutting"))
            return;

        var uid = entity.Owner;
        var user = args.User;
        var used = args.Using.Value;
        // Construct verb object.
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                AttemptShear(uid, user, used);
            },
            Text = Loc.GetString("wooly-system-verb-shear"),
            Priority = 2
        };
        // Add verb to the player.
        args.Verbs.Add(verb);
    }
}
