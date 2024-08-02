using Content.Server.Animals.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Shearing;
using Content.Shared.Verbs;
using Content.Shared.DoAfter;
using Content.Shared.Tools.Systems;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;
using Content.Server.Stack;
using Content.Shared.Stacks;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;

namespace Content.Server.Animals.Systems;

/// <summary>
///     Lets an entity be sheared by a tool to consume a reagent to spawn an amount of an item.
///     For example, sheep can be sheared to consume woolSolution to spawn cotton.
/// </summary>
public sealed class ShearableSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShearableComponent, GetVerbsEvent<AlternativeVerb>>(AddShearVerb);
        SubscribeLocalEvent<ShearableComponent, ShearingDoAfterEvent>(OnSheared);
    }

    /// <summary>
    ///     Attempts to shear the target animal, checking if it is shearable and building arguments for calling TryStartDoAfter.
    ///     Called by the "shear" verb.
    /// </summary>
    private void AttemptShear(Entity<ShearableComponent?> shearable, EntityUid userUid, EntityUid containerUid)
    {
        // Check the target creature has the shearable component.
        if (!Resolve(shearable, ref shearable.Comp))
            return;

        // Build arguments for calling TryStartDoAfter
        var doargs = new DoAfterArgs(
            EntityManager,
            userUid,
            5,
            new ShearingDoAfterEvent(),
            shearable,
            shearable,
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
    ///     Checks that the target shearable creature contains a shearable solution.
    ///     Performs solution calcuations, then creates a corresponding pop-up message, and if successful spawns shearedProductID under the shearable creature.
    /// </summary>
    private void OnSheared(Entity<ShearableComponent> ent, ref ShearingDoAfterEvent args)
    {
        // Check the action hasn't been cancelled, already handled, and that there's an item in the player's hand.
        if (args.Cancelled || args.Handled || args.Args.Used == null)
            return;

        // Resolves the targetSolutionName as a solution inside the shearable creature. Outputs the "solution" variable.
        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.TargetSolutionName, ref ent.Comp.Solution, out var solution))
            return;

        // Mark as handled so we don't duplicate.
        args.Handled = true;

        // Store solution.Volume in a variable to make calculations a bit clearer.
        var targetSolutionQuantity = solution.Volume;

        // Create a stack object so we can reference its name in localisation.
        _prototypeManager.TryIndex<StackPrototype>(ent.Comp.ShearedProductID, out var shearedProductStack);
        if (shearedProductStack == null)
        {
            throw new Exception("Could not resolve shearedProductID to a StackPrototype.");
        }

        // Failure message, if the shearable creature has no targetSolutionName to be sheared.
        if (targetSolutionQuantity == 0)
        {
            _popup.PopupEntity(
                Loc.GetString(
                    "shearable-system-no-product",
                    (
                        "target",
                        Identity.Entity(
                            ent.Owner,
                            EntityManager
                        )
                    ),
                    (
                        "product",
                        shearedProductStack.Name
                    )
                ),
                ent.Owner,
                args.Args.User
            );
            return;
        }

        // Solution is measured in units but the actual value for 1u is 1000 reagent, so multiply it by 100.
        // Then, divide by 1 because it's the reagent needed for 1 product.
        var productsPerSolution = (int)(1 / ent.Comp.ProductsPerSolution * 100);


        // Work out the maxium stack size of the product.
        var maxProductsToSpawnValue = 0;
        var maxProductsToSpawn = _prototypeManager.Index((ProtoId<StackPrototype>)ent.Comp.ShearedProductID).MaxCount;
        if (maxProductsToSpawn.HasValue)
        {
            maxProductsToSpawnValue = maxProductsToSpawn.Value;
        }

        // Modulas the targetSolutionQuantity so no solution is wasted if it can't be divided evenly.
        // Everything is divided by 100, because for fixedPoint2 multiplies everything by 100.
        // Math.Min ensures that no more solution than what is needed for the maximum stack is used, shear the entity multiple times if you want the rest of the product.
        var solutionToRemove = FixedPoint2.New(Math.Min((targetSolutionQuantity.Value - targetSolutionQuantity.Value % productsPerSolution) / 100, maxProductsToSpawnValue * productsPerSolution / 100));

        // Split the solution inside the creature by solutionToRemove, return what was removed.
        var removedSolution = _solutionContainer.SplitSolution(ent.Comp.Solution.Value, solutionToRemove);

        // Target the creature's location.
        var spawnCoordinates = Transform(ent).Coordinates;

        // Spawn cotton.
        _stackSystem.Spawn(removedSolution.Volume.Value / productsPerSolution, ent.Comp.ShearedProductID, spawnCoordinates);

        // Success message.
        _popup.PopupEntity(
            Loc.GetString(
                "shearable-system-success",
                (
                    "target",
                    Identity.Entity(
                        ent.Owner,
                        EntityManager
                    )
                ),
                (
                    "product",
                    shearedProductStack.Name
                )
            ),
            ent.Owner,
            args.Args.User,
            PopupType.Medium
        );
    }

    /// <summary>
    ///     Adds the "shear" verb to the player.
    ///     Checks first if the player is holding an item with the specified toolQuality.
    /// </summary>
    private void AddShearVerb(Entity<ShearableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {

        if (args.Using == null ||
             !args.CanInteract ||
             // Checks if you're using an item with the toolQuality component quality.
             !_tool.HasQuality(args.Using.Value, ent.Comp.ToolQuality))
            return;

        var uid = ent.Owner;
        var user = args.User;
        var used = args.Using.Value;
        // Construct verb object.
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                AttemptShear(uid, user, used);
            },
            Text = Loc.GetString("shearable-system-verb-shear"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/scissors.svg.236dpi.png")),
            Priority = 2
        };
        // Add verb to the player.
        args.Verbs.Add(verb);
    }
}
