using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Shearing;
using Content.Shared.Tools.Systems;
using Content.Shared.Toggleable;
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
    [Dependency]
    private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShearableComponent, GetVerbsEvent<AlternativeVerb>>(AddShearVerb);
        SubscribeLocalEvent<ShearableComponent, InteractUsingEvent>(OnClicked);
        SubscribeLocalEvent<ShearableComponent, ExaminedEvent>(Examined);
        SubscribeLocalEvent<ShearableComponent, ShearingDoAfterEvent>(OnSheared);
        SubscribeLocalEvent<ShearableComponent, SolutionContainerChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<ShearableComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    /// <summary>
    ///     Checks if the target entity can currently be sheared.
    /// </summary>
    /// <param name="ent">The shearable entity that will be checked.</param>
    /// <param name="comp">The shearable component (e.g. ent.Comp).</param>
    /// <param name="usedItem">The held item that is being used to shear the target entity.</param>
    /// <param name="checkItem">If false then skip checking for the correct shearing tool.</param>
    /// <returns>
    ///     A <c>ShearableComponent.CheckShearReturns</c> enum of the result.
    /// </returns>
    /// <seealso cref="ShearableComponent.CheckShearReturns"/>
    public ShearableComponent.CheckShearReturns CheckShear(
        EntityUid ent,
        ShearableComponent comp,
        EntityUid? usedItem = null,
        bool checkItem = true
    )
    {
        // If false then skip checking for a tool, otherwise return on wrong tool.
        if (checkItem)
        {
            if (
                // Is the player holding an item?
                usedItem == null
                ||
                // Does the held item have the correct toolQuality component quality?
                !_tool.HasQuality((EntityUid)usedItem, comp.ToolQuality)
            )
                return ShearableComponent.CheckShearReturns.WrongTool;
        }

        // Test if the configured product exists.
        if (!_prototypeManager.TryIndex(comp.ShearedProductID, out var _))
        {
            return ShearableComponent.CheckShearReturns.ProductError;
        }

        // Everything below this point is just calculating whether the animal
        // has enough solution to spawn at least one item in the specified stack.
        // If so, True, otherwise False.

        // Resolves the targetSolutionName as a solution inside the shearable creature. Outputs the "solution" variable.
        if (
            !_solutionContainer.ResolveSolution(
                ent,
                comp.TargetSolutionName,
                ref comp.Solution,
                out var solution
            )
        )
            return ShearableComponent.CheckShearReturns.SolutionError;

        // Store solution.Volume in a variable to make calculations a bit clearer.
        var targetSolutionQuantity = solution.Volume;

        /*         // Create a stack object so we can reference its name in localisation.
                _prototypeManager.TryIndex(comp.ShearedProductID, out var shearedProductStack);
                if (shearedProductStack == null)
                {
                    Log.Error(
                        $"Could not resolve ShearedProductID \"{comp.ShearedProductID}\" to a StackPrototype while shearing. Is this item stackable?"
                    );
                    return ShearableComponent.CheckShearReturns.StackError;
                } */


        // Spawn maximum of 25 items
        // If less than 25 items, calculate solution to remove.
        // If more than 25 items, cap at the calculated solution.

        // Solution is measured in units but the actual value for 1u is 1000 reagent, so multiply it by 100.
        // Then, divide by 1 because it's the reagent needed for 1 product.
        var productsPerSolution = (int)(1 / comp.ProductsPerSolution * 100);

        // Work out the maximum number of products to spawn.
        var maxProductsToSpawn = (float)productsPerSolution;
        // If a limit has been defined, use that.
        if (comp.MaximumProductsSpawned is not null)
        {
            // No limit defined, so set to productsPerSolution
            maxProductsToSpawn = productsPerSolution;
        }

        // Modulas the targetSolutionQuantity so no solution is wasted if it can't be divided evenly.
        // Everything is divided by 100, because fixedPoint2 multiplies everything by 100.
        // Math.Min ensures that no more solution than what is needed for the maximum stack is used, shear the entity multiple times if you want the rest of the product.
        var solutionToRemove = FixedPoint2.New(
            Math.Min(
                (targetSolutionQuantity.Value - targetSolutionQuantity.Value % productsPerSolution) / 100,
                maxProductsToSpawn * productsPerSolution / 100
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
                // Resolve the prototype so we can reference its name in localisation.
                if (!_prototypeManager.TryIndex(ent.Comp.ShearedProductID, out var proto))
                    return;
                // NO WOOL LEFT.
                _popup.PopupClient(
                    Loc.GetString(
                        "shearable-system-no-product",
                        ("target", Identity.Entity(ent.Owner, EntityManager)),
                        ("product", proto.Name)
                    ),
                    ent.Owner,
                    userUid
                );
                return;
            case ShearableComponent.CheckShearReturns.ProductError:
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
    ///     Called by the ShearingDoAfter event.
     /// </summary>
    private void OnSheared(Entity<ShearableComponent> ent, ref ShearingDoAfterEvent args)
    {

        // Check the action hasn't been cancelled, or hasn't already been handled, or that the player's hand is empty.
        if (args.Cancelled || args.Handled)
            return;

        // Resolves the targetSolutionName as a solution inside the shearable creature. Outputs the "solution" variable.
        if (
            !_solutionContainer.ResolveSolution(
                ent.Owner,
                ent.Comp.TargetSolutionName,
                ref ent.Comp.Solution,
                out var solution
            )
        )
            return;

        // Resolve the ShearedProductID so we can get the details.
        // Also check if the specified product actually exists and can be spawned.
        if (!_prototypeManager.TryIndex(ent.Comp.ShearedProductID, out var proto))
            return;

        // Mark as handled so we don't duplicate.
        args.Handled = true;

        // Store solution.Volume in a variable to make calculations a bit clearer.
        var targetSolutionQuantity = solution.Volume;

        // Solution is measured in units but the actual value for 1u is 1000 reagent, so multiply it by 100.
        // Then, divide by 1 because it's the reagent needed for 1 product.
        var productsPerSolution = (int)(1 / ent.Comp.ProductsPerSolution * 100);

        // Work out the maximum number of products to spawn.
        var maxProductsToSpawn = (float)productsPerSolution;
        // If a limit has been defined, use that.
        if (ent.Comp.MaximumProductsSpawned is not null)
        {
            // No limit defined, so set to productsPerSolution
            maxProductsToSpawn = productsPerSolution;
        }

        // Modulas the targetSolutionQuantity so no solution is wasted if it can't be divided evenly.
        // subtract targetSolutionQuantity from the remainder.
        // Everything is divided by 100, because fixedPoint2 multiplies everything by 100.
        // Math.Min ensures that no more solution than what is needed for the maximum stack is used, shear the entity multiple times if you want the rest of the product.
        // e.g.
        // Sheep contains 5000 fibre reagent (50 units).
        // We want 0.2 products per solution. Since we're calcuating with reagent and not units we need to modify the value.
        // 1 / 0.2 * 100 = 500. (See above)
        // 5000 - 5000 % 500. 500 fits nicely into 5000 so there is no remainer of 0. This means we're removing all 5000 reagent currently.
        // Next we check the maxmium number of product we want to spawn, if this is less than 5000 then the animal will need to be sheared multiple times to delete its resources.
        // We've not configured a maxProductsToSpawn, so we aren't imposing a limit. In this case it defaults to productsPerSolution.
        // productsPerSolution is set to 500. Therefore, the calculation is:
        // 500 * 500 / 100 = 2500.
        // We take the smaller of two values, we don't want to remove more reagent than we're using.
        // Despite their being 5000 reagent availble, we end up only removing 2500, even though no limit has been set, why is this?
        // I don't know.
        var solutionToRemove = FixedPoint2.New(
            Math.Min(
                (targetSolutionQuantity.Value - targetSolutionQuantity.Value % productsPerSolution) / 100,
                maxProductsToSpawn * productsPerSolution / 100
            )
        );

        // Failure message, if the shearable creature has no targetSolutionName to be sheared.
        if (solutionToRemove == 0)
        {
            _popup.PopupClient(
                Loc.GetString(
                    "shearable-system-no-product",
                    ("target", Identity.Entity(ent.Owner, EntityManager)),
                    ("product", proto.Name)
                ),
                ent.Owner,
                args.Args.User
            );
            return;
        }

        // Split the solution inside the creature by solutionToRemove, return what was removed.
        var removedSolution = _solutionContainer.SplitSolution(ent.Comp.Solution.Value, solutionToRemove);

        // Spawn product.
        for (var i = 0; i < removedSolution.Volume.Value / productsPerSolution; i++)
        {

            EntityManager.PredictedSpawnNextToOrDrop(ent.Comp.ShearedProductID, ent);
        }

        // Success message.
        _popup.PopupClient(
            Loc.GetString(
                "shearable-system-success",
                ("target", Identity.Entity(ent.Owner, EntityManager)),
                ("product", proto.Name)
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

    /// <summary>
    ///     This function adds status hints to the examine of a shearable entity.
    ///     They indicate whether the entity can be sheared or not.
    /// </summary>
    /// <param name="ent">the entity containing a wooly component that will be checked.</param>
    /// <param name="args">Arguments passed through by the ExaminedEvent.
    private void Examined(Entity<ShearableComponent> ent, ref ExaminedEvent args)
    {
        // Shearable description additions are optional, return if unset.
        // Saves some time if neither have been configured.
        if (string.IsNullOrEmpty(ent.Comp.ShearableMarkupText) && string.IsNullOrEmpty(ent.Comp.UnShearableMarkupText))
        {
            return;
        }

        // Checks whether the entity can be sheared and applies appropriate examine additions.
        switch (CheckShear(ent, ent.Comp, checkItem: false))
        {
            case ShearableComponent.CheckShearReturns.Success:
                // Check again if this description has been set.
                if (string.IsNullOrEmpty(ent.Comp.ShearableMarkupText))
                {
                    break;
                }
                // ALL SYSTEMS GO!
                args.PushMarkup(
                    Loc.GetString(
                        ent.Comp.ShearableMarkupText,
                        ("target", Identity.Entity(ent.Owner, EntityManager)),
                        ("toolQuality", ent.Comp.ToolQuality.ToLower())
                    )
                );
                return;
            case ShearableComponent.CheckShearReturns.SolutionError:
                return;
            case ShearableComponent.CheckShearReturns.StackError:
                return;
            case ShearableComponent.CheckShearReturns.InsufficientSolution:
                // Check again if this description has been set.
                if (string.IsNullOrEmpty(ent.Comp.UnShearableMarkupText))
                {
                    break;
                }
                args.PushMarkup(
                    Loc.GetString(
                        ent.Comp.UnShearableMarkupText,
                        ("target", Identity.Entity(ent.Owner, EntityManager))
                    )
                );
                return;
            case ShearableComponent.CheckShearReturns.ProductError:
                return;
        }
    }

    /// <summary>
    ///     Used for managing the shearing layer as the shearable solution levels change.
    ///     e.g. in Sheep, it will remove the wooly layer when the remaining reagent in the wool solution drops to 0.
    ///     the layer is re-added when the reagent is above 0.
    ///     Check the sheep's Sprite and GenericVisualizer components for an example of how to add a shearable layer to your animal.
    /// </summary>
    /// <param name="ent">the entity containing a wooly component that will be checked.</param>
    /// <param name="args">Arguments passed through by the ExaminedEvent.
    private void OnSolutionChange(Entity<ShearableComponent> ent, ref SolutionContainerChangedEvent args)
    {
        // Only interested in wool solution, ignore the rest.
        if (args.SolutionId != ent.Comp.TargetSolutionName)
            return;

        UpdateShearingLayer(ent, args.Solution);
    }

    /// <summary>
    ///     This function checks the entity's wool solution and either disables or enables the wool layer (if one exists).
    /// </summary>
    /// <param name="ent">the entity containing a wooly component that will be checked.</param>
    /// <param name="sol">a resolved solution object the presence of which will be checked.
    private void UpdateShearingLayer(Entity<ShearableComponent> ent, Solution? sol = null)
    {
        // If the sol parameter hasn't been provided, we'll try to grab the solution from inside the animal instead.
        Solution? solution;
        if (sol is null)
        {
            if (!_solutionContainer.ResolveSolution(
                ent.Owner,
                ent.Comp.TargetSolutionName,
                ref ent.Comp.Solution,
                out solution
            ))
                // Somehow, this entity has no shearing solution.
                return;

        }
        else
        {
            solution = sol;
        }

        // appearance is used to disable and enable the wool layer.
        if (!TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            return;
        // mState is used to check if the animal is dead/critical.
        TryComp<MobStateComponent>(ent.Owner, out var mobState);

        // If we couldn't resolve the mobState for some reason then just assume it's alive.
        mobState ??= new MobStateComponent();

        // If there's no solution at all, or the entity is dead or critical, remove the wool layer.
        // Otherwise, enable it.
        if (solution.Volume.Value <= 0)
        {
            // Remove wool layer
            _appearance.SetData(ent.Owner, ToggleableVisuals.Enabled, false, appearance);

        }
        else
        {
            // Add wool layer
            _appearance.SetData(ent.Owner, ToggleableVisuals.Enabled, true, appearance);
        }
    }

    /// <summary>
    ///     This is used for checking if the shearable animal is dead or critical.
    ///     If it is, then the shearing layer is removed.
    /// </summary>
    private void OnMobStateChanged(Entity<ShearableComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateShearingLayer(ent);
    }

}
