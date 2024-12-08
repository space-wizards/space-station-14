using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Shearing;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Animals;

/// <summary>
///     Lets an entity be sheared by a tool to consume a reagent to spawn an amount of an item.
///     For example, sheep can be sheared to consume woolSolution to spawn cotton.
/// </summary>
public sealed class SharedShearableSystem : EntitySystem
{
    [Dependency]
    private readonly SharedToolSystem _tool = default!;

    [Dependency]
    private readonly SharedDoAfterSystem _doAfterSystem = default!;

    [Dependency]
    private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    [Dependency]
    private readonly IPrototypeManager _prototypeManager = default!;

    [Dependency]
    private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShearableComponent, GetVerbsEvent<AlternativeVerb>>(AddShearVerb);
        SubscribeLocalEvent<ShearableComponent, InteractUsingEvent>(OnClicked);
    }

    /// <summary>
    ///     Checks if the target entity can currently be sheared.
    /// </summary>
    /// <param name="targetEntity">The shearable entity that will be checked.</param>
    /// <param name="comp">The shearable component (e.g. ent.Comp).</param>
    /// <param name="usedItem">The held item that is being used to shear the target entity.</param>
    /// <returns>
    ///     A <c>ShearableComponent.CheckShearReturns</c> enum of the result.
    /// </returns>
    /// <seealso cref="ShearableComponent.CheckShearReturns"/>
    public ShearableComponent.CheckShearReturns CheckShear(
        EntityUid targetEntity,
        ShearableComponent comp,
        EntityUid? usedItem
    )
    {
        if (
            // Is the player holding an item?
            usedItem == null
            ||
            // Does the held item have the correct toolQuality component quality?
            !_tool.HasQuality((EntityUid)usedItem, comp.ToolQuality)
        )
            return ShearableComponent.CheckShearReturns.WrongTool;

        // Everything below this point is just calculating whether the animal
        // has enough solution to spawn at least one item in the specified stack.
        // If so, True, otherwise False.

        // Resolves the targetSolutionName as a solution inside the shearable creature. Outputs the "solution" variable.
        if (
            !_solutionContainer.ResolveSolution(
                targetEntity,
                comp.TargetSolutionName,
                ref comp.Solution,
                out var solution
            )
        )
            return ShearableComponent.CheckShearReturns.SolutionError;

        // Store solution.Volume in a variable to make calculations a bit clearer.
        var targetSolutionQuantity = solution.Volume;

        // Create a stack object so we can reference its name in localisation.
        _prototypeManager.TryIndex(comp.ShearedProductID, out var shearedProductStack);
        if (shearedProductStack == null)
        {
            Log.Error(
                $"Could not resolve ShearedProductID \"{comp.ShearedProductID}\" to a StackPrototype while shearing. Does this item exist?"
            );
            return ShearableComponent.CheckShearReturns.StackError;
        }

        // Solution is measured in units but the actual value for 1u is 1000 reagent, so multiply it by 100.
        // Then, divide by 1 because it's the reagent needed for 1 product.
        var productsPerSolution = (int)(1 / comp.ProductsPerSolution * 100);

        // Work out the maxium stack size of the product.
        var maxProductsToSpawnValue = 0;
        var maxProductsToSpawn = _prototypeManager.Index(comp.ShearedProductID).MaxCount;
        if (maxProductsToSpawn.HasValue)
        {
            maxProductsToSpawnValue = maxProductsToSpawn.Value;
        }

        // Modulas the targetSolutionQuantity so no solution is wasted if it can't be divided evenly.
        // Everything is divided by 100, because for fixedPoint2 multiplies everything by 100.
        // Math.Min ensures that no more solution than what is needed for the maximum stack is used, shear the entity multiple times if you want the rest of the product.
        var solutionToRemove = FixedPoint2.New(
            Math.Min(
                (targetSolutionQuantity.Value - targetSolutionQuantity.Value % productsPerSolution) / 100,
                maxProductsToSpawnValue * productsPerSolution / 100
            )
        );
        // Failure message, if the shearable creature has no targetSolutionName to be sheared.
        if (solutionToRemove <= 0)
        {
            return ShearableComponent.CheckShearReturns.InsufficientSolution;
        }

        return ShearableComponent.CheckShearReturns.Success;
    }

    /// <summary>
    ///     Handles shearing when the player left-clicks an entity.
    ///     Doesn't run any checks, those are handled by AttemptShear.
    /// </summary>
    private void OnClicked(Entity<ShearableComponent> ent, ref InteractUsingEvent args)
    {
        // All checks run from AttemptShear.
        AttemptShear(ent, args.User, args.Used);
    }

    /// <summary>
    ///     Attempts to shear the target animal, checking if it is shearable and building arguments for calling TryStartDoAfter.
    ///     Called by the "shear" verb.
    /// </summary>
    private void AttemptShear(Entity<ShearableComponent> ent, EntityUid userUid, EntityUid toolUsed)
    {
        // Run all shearing checks.
        switch (CheckShear(ent, ent.Comp, toolUsed))
        {
            case ShearableComponent.CheckShearReturns.Success:
                // ALL SYSTEMS GO!
                break;
            case ShearableComponent.CheckShearReturns.WrongTool:
                return;
            case ShearableComponent.CheckShearReturns.SolutionError:
                return;
            case ShearableComponent.CheckShearReturns.StackError:
                return;
            case ShearableComponent.CheckShearReturns.InsufficientSolution:
                // Create a stack object so we can reference its name in localisation.
                _prototypeManager.TryIndex(ent.Comp.ShearedProductID, out var shearedProductStack);
                if (shearedProductStack == null)
                {
                    // Whatever, this animal has an invalid product defined and it's already logged so just fail silently.
                    return;
                }
                // NO WOOL LEFT.
                _popup.PopupClient(
                    Loc.GetString(
                        "shearable-system-no-product",
                        ("target", Identity.Entity(ent.Owner, EntityManager)),
                        ("product", shearedProductStack.Name)
                    ),
                    ent.Owner,
                    userUid
                );
                return;
        }

        // Build arguments for calling TryStartDoAfter
        var doargs = new DoAfterArgs(EntityManager, userUid, 5, new ShearingDoAfterEvent(), ent, ent, used: toolUsed)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1.0f,
        };

        // Triggers the ShearingDoAfter event.
        _doAfterSystem.TryStartDoAfter(doargs);
    }

    /// <summary>
    ///     Adds the "shear" verb to the player.
    ///     Checks first if the player is holding an item with the specified toolQuality.
    /// </summary>
    private void AddShearVerb(Entity<ShearableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (
            args.Using == null
            || !args.CanInteract
            ||
            // Checks if you're using an item with the toolQuality component quality.
            !_tool.HasQuality(args.Using.Value, ent.Comp.ToolQuality)
        )
            return;

        var uid = ent.Owner;
        var user = args.User;
        var used = args.Using.Value;
        // Construct verb object.
        AlternativeVerb verb =
            new()
            {
                Act = () =>
                {
                    AttemptShear(ent, user, used);
                },
                Text = Loc.GetString("shearable-system-verb-shear"),
                Icon = new SpriteSpecifier.Texture(
                    new ResPath("/Textures/Interface/VerbIcons/scissors.svg.236dpi.png")
                ),
                Priority = 2
            };
        // Add verb to the player.
        args.Verbs.Add(verb);
    }
}
