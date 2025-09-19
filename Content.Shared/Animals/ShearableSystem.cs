using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared.Animals;

/// <summary>
///     Lets an entity be sheared by a tool to consume a reagent to spawn an amount of an item and optionally toggle a sprite layer.
///     For example, sheep can be sheared to consume woolSolution to spawn cotton.
/// </summary>
public sealed class SharedShearableSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShearableComponent, GetVerbsEvent<AlternativeVerb>>(AddShearVerb);
        SubscribeLocalEvent<ShearableComponent, InteractUsingEvent>(OnInteractUsingEvent);
        SubscribeLocalEvent<ShearableComponent, ExaminedEvent>(Examined);
        SubscribeLocalEvent<ShearableComponent, ShearingDoAfterEvent>(OnSheared);
        SubscribeLocalEvent<ShearableComponent, SolutionContainerChangedEvent>(OnSolutionChange);
    }

    /// <summary>
    ///     Checks if the target entity can currently be sheared.
    /// </summary>
    /// <param name="ent">The shearable entity that will be checked.</param>
    /// <param name="comp">The shearable component (e.g. ent.Comp).</param>
    /// <param name="shearedProduct">An out variable of the resolved sheared product prototype.</param>
    /// <param name="shearingSolutionState">An out variable of the resolved sheared product solution.</param>
    /// <param name="shearingSolutionEntity">An out variable of the resolved sheared product solution entity.</param>
    /// <param name="usedItem">The held item that is being used to shear the target entity.</param>
    /// <param name="checkItem">If false then skip checking for the correct shearing tool.</param>
    /// <returns>
    ///     A <c>ShearableComponent.CheckShearReturns</c> enum of the result.
    /// </returns>
    /// <seealso cref="CheckShearReturns"/>
    public CheckShearReturns CheckShear(EntityUid ent, ShearableComponent comp, out EntityPrototype shearedProduct, out Solution? shearingSolutionState, out Entity<SolutionComponent>? shearingSolutionEnt, EntityUid? usedItem = null, bool checkItem = true)
    {
        // Set these to null in-case we return early.
        shearedProduct = _proto.Index(comp.ShearedProductID);
        shearingSolutionState = null;
        shearingSolutionEnt = null;

        // Are we checking items? Has a toolQuality been defined?
        // Even if we are checking items, if no toolQuality has been defined, then they're allowed to use anything, including an empty hand.
        if (checkItem && comp.ToolQuality is not null &&
            // If so, is the player holding anything at all, and does that item have the correct toolQuality?
            (usedItem == null || !_tool.HasQuality(usedItem.Value, comp.ToolQuality)))
        {
            return CheckShearReturns.WrongTool;
        }


        // Everything below this point is just calculating whether the animal
        // has enough solution to spawn at least one item in the specified stack.
        // If so, True, otherwise False.

        // Resolves the targetSolutionName as a solution inside the shearable creature. Outputs the "solution" variable.
        if (!_solutionContainer.ResolveSolution(ent, comp.TargetSolutionName, ref shearingSolutionEnt, out shearingSolutionState))
        {
            return CheckShearReturns.Error;
        }

        // Store solution.Volume in a variable to make calculations a bit clearer.
        var targetSolutionQuantity = shearingSolutionState.Volume;

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
            return CheckShearReturns.InsufficientSolution;
        }

        return CheckShearReturns.Success;
    }

    /// <summary>
    ///     Handles shearing when the player left-clicks an entity.
    ///     Doesn't run any checks, those are handled by AttemptShear.
    /// </summary>
    private void OnInteractUsingEvent(Entity<ShearableComponent> ent, ref InteractUsingEvent args)
    {
        // If no tool is specified then this might take over empty-hand interaction of an entity.
        // But, sheep have a default petting action so presumably this can also be overidden up the hierarchy.
        // All checks run from AttemptShear.
        AttemptShear(ent, args.User, args.Used);
    }

    /// <summary>
    ///     Attempts to shear the target animal, checking if it is shearable and building arguments for calling TryStartDoAfter.
    ///     Called by the "shear" verb.
    /// </summary>
    private void AttemptShear(Entity<ShearableComponent> ent, EntityUid userUid, EntityUid? toolUsed)
    {
        // Run all shearing checks.
        switch (CheckShear(ent, ent.Comp, out var shearedProduct, out _, out _, toolUsed))
        {
            case CheckShearReturns.Success:
                // ALL SYSTEMS GO!
                break;
            case CheckShearReturns.WrongTool:
                // Removed because it would cause excessive popups when e.g. you're feeding the sheep some food.
                //_popup.PopupClient(Loc.GetString("shearable-system-wrong-tool", ("target", Identity.Entity(ent.Owner, EntityManager)), ("shearVerb", (Loc.GetString(ent.Comp.Verb)).ToLower())), ent.Owner, userUid);
                return;
            case CheckShearReturns.InsufficientSolution:
                // NO WOOL LEFT.
                _popup.PopupClient(Loc.GetString("shearable-system-no-product", ("target", Identity.Entity(ent.Owner, EntityManager)), ("product", shearedProduct.Name)), ent.Owner, userUid);
                return;
        }

        // Build arguments for calling TryStartDoAfter
        var doArgs = new DoAfterArgs(EntityManager, userUid, 5, new ShearingDoAfterEvent(), ent, ent, used: toolUsed)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1.0f,
        };

        // Triggers the ShearingDoAfter event.
        _doAfter.TryStartDoAfter(doArgs);
    }

    /// <summary>
    ///     Called by the ShearingDoAfter event.
    /// </summary>
    private void OnSheared(Entity<ShearableComponent> ent, ref ShearingDoAfterEvent args)
    {
        // Check the action hasn't been cancelled, or hasn't already been handled, or that the player's hand is empty.
        if (args.Cancelled || args.Handled)
            return;

        // Check again and this time get the objects we need.
        if (CheckShear(ent.Owner, ent.Comp, out var shearedProduct, out var shearingSolutionState, out var shearingSolutionEnt, null, false) != CheckShearReturns.Success
            // shearingSolution must be resolved.
            || shearingSolutionState is null
            || shearingSolutionEnt is null)
        {
            return;
        }

        // Mark as handled so we don't duplicate.
        args.Handled = true;

        // Store solution.Volume in a variable to make calculations a bit clearer.
        var targetSolutionQuantity = shearingSolutionState.Volume;

        // Solution is measured in units but the actual value for 1u is 1000 reagent, so multiply it by 100.
        // Then, divide by 1 because it's the reagent needed for 1 product.
        var productsPerSolution = (int)(1 / ent.Comp.ProductsPerSolution * 100);

        // Work out the maximum number of products to spawn.
        var maxProductsToSpawn = (float)productsPerSolution;
        // If a limit has been defined, use that.
        if (ent.Comp.MaximumProductsSpawned is not null)
        {
            // No limit defined, so set to productsPerSolution
            maxProductsToSpawn = (float)ent.Comp.MaximumProductsSpawned;
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
        // Despite their being 5000 reagent available, we end up only removing 2500, even though no limit has been set, why is this?
        // I don't know... it seems to work OK though.
        var solutionToRemove = FixedPoint2.New(
            Math.Min(
                (targetSolutionQuantity.Value - targetSolutionQuantity.Value % productsPerSolution) / 100,
                maxProductsToSpawn * productsPerSolution / 100
            )
        );

        // Failure message, if the shearable creature has no targetSolutionName to be sheared.
        if (solutionToRemove == 0)
        {
            _popup.PopupClient(Loc.GetString("shearable-system-no-product", ("target", Identity.Entity(ent.Owner, EntityManager)), ("product", shearedProduct.Name)), ent.Owner, args.Args.User);
            return;
        }

        // Split the solution inside the creature by solutionToRemove, return what was removed.
        var removedSolution = _solutionContainer.SplitSolution(shearingSolutionEnt.Value, solutionToRemove);

        // Psuedo shared randomness stolen from #39661
        // Can be replaced with SharedRandom once that exists.
        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var random = new System.Random(seed);

        var center = ent.Owner.ToCoordinates();
        // Spawn product.
        for (var i = 0; i < removedSolution.Volume.Value / productsPerSolution; i++)
        {
            // Offset the spawn position by 0.4 pixels, so they don't all stack in one spot.
            var xoffs = random.NextFloat(-0.2f, 0.2f);
            var yoffs = random.NextFloat(-0.2f, 0.2f);
            var pos = center.Offset(new Vector2(xoffs, yoffs));

            EntityManager.PredictedSpawnAtPosition(ent.Comp.ShearedProductID, pos);
        }

        // Success message.
        _popup.PopupClient(Loc.GetString("shearable-system-success", ("target", Identity.Entity(ent.Owner, EntityManager)), ("product", shearedProduct.Name)), ent.Owner, args.Args.User, PopupType.Medium);
    }

    /// <summary>
    ///     Adds the "shear" verb to the player.
    ///     Checks first if the player is holding an item with the specified toolQuality.
    /// </summary>
    private void AddShearVerb(Entity<ShearableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        // Check if you are allowed to interact currently.
        if (!args.CanInteract)
            return;

        // If we're not using a tool then Using will be null so we need to check it quickly.
        EntityUid? toolUsed = null;
        if (args.Using is not null)
        {
            toolUsed = args.Using.Value;
        }

        // Check.
        if (CheckShear(ent.Owner, ent.Comp, out _, out _, out _, toolUsed, true) != CheckShearReturns.Success)
        {
            return;
        }

        var user = args.User;

        // Construct verb object.
        AlternativeVerb verb =
            new()
            {
                Act = () => AttemptShear(ent, user, toolUsed),
                Text = Loc.GetString(ent.Comp.Verb),
                Icon = ent.Comp.ShearingIcon,
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
    /// <param name="args">Arguments passed through by the ExaminedEvent.</param>
    private void Examined(Entity<ShearableComponent> ent, ref ExaminedEvent args)
    {
        // Shearable description additions are optional, return if unset.
        // Saves some time if neither have been configured.
        if (string.IsNullOrEmpty(ent.Comp.ShearableMarkupText) && string.IsNullOrEmpty(ent.Comp.UnShearableMarkupText))
        {
            return;
        }

        // Checks whether the entity can be sheared and applies appropriate examine additions.
        switch (CheckShear(ent, ent.Comp, out _, out _, out _, checkItem: false))
        {
            case CheckShearReturns.Success:
                // Check again if this description has been set.
                if (string.IsNullOrEmpty(ent.Comp.ShearableMarkupText))
                {
                    break;
                }
                // Default to empty string, if we just can't resolve the tool quality for whatever reason localisation have a blank variable..
                var toolQuality = string.Empty;
                // If a ToolQuality has been specified set its name to toolQuality so it appears in localisation.
                if (_proto.TryIndex(ent.Comp.ToolQuality, out var toolQualityProto, false))
                {
                    // Tool quality names are a Loc string so look up that and lower-case it.
                    toolQuality = Loc.GetString(toolQualityProto.Name).ToLower();
                    // If a Loc string isn't found then it will just return the same ID, which means it hasn't been configured right so just error and return.
                    if (string.Equals(toolQuality, toolQualityProto.Name.ToLower()))
                    {
                        Log.Debug($"Tried to generate examine text for a shearable entity \"{Name(ent.Owner)}\" but the configured toolQuality ({toolQualityProto.ID}) name: \"{toolQuality}\" is not a Loc string.");
                        return;
                    }
                }
                // ALL SYSTEMS GO!
                args.PushMarkup(Loc.GetString(ent.Comp.ShearableMarkupText, ("target", Identity.Entity(ent.Owner, EntityManager)), ("toolQuality", toolQuality)));
                return;
            case CheckShearReturns.InsufficientSolution:
                // Check again if this description has been set.
                if (string.IsNullOrEmpty(ent.Comp.UnShearableMarkupText))
                {
                    break;
                }
                args.PushMarkup(Loc.GetString(ent.Comp.UnShearableMarkupText, ("target", Identity.Entity(ent.Owner, EntityManager))));
                return;
        }
    }

    /// <summary>
    ///     This function changes the animal's shearable layer based on the solution volume.
    ///     e.g. when a sheep's wool solution volume drops below 5, which is the minimum needed to shear it, the wool will disapear.
    /// </summary>
    /// <param name="ent">the entity containing a wooly component that will be checked.</param>
    /// <param name="sol">a SolutionContainerChangedEvent object passed by the OnSolutionChange event.</param>
    private void UpdateShearingLayer(Entity<ShearableComponent> ent, Solution sol)
    {

        // appearance is used to disable and enable the wool layer.
        if (!TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            return;

        // The minimum solution required to spawn one product.
        var minimumSol = 1 / ent.Comp.ProductsPerSolution;

        // If solution is less than the minimum then disable the shearable layer.
        if (sol.Volume.Value < minimumSol * 100)
        {
            // Remove wool layer
            _appearance.SetData(ent.Owner, ShearableVisuals.Shearable, false, appearance);
        }
        // If solution is more than the minimum then disable the shearable layer.
        else
        {
            // Add wool layer
            _appearance.SetData(ent.Owner, ShearableVisuals.Shearable, true, appearance);
        }
    }

    /// <summary>
    ///     Listens for changes in solution, checks if it's a wooly solution, and passes it to UpdateShearingLayer.
    ///     Depending on the result, the wooly layer may change.
    /// </summary>
    /// <param name="ent">the entity containing a wooly component that will be checked.</param>
    /// <param name="args">Arguments passed through by the ExaminedEvent.</param>
    private void OnSolutionChange(Entity<ShearableComponent> ent, ref SolutionContainerChangedEvent args)
    {
        // Only interested in wool solution, ignore the rest.
        if (args.SolutionId != ent.Comp.TargetSolutionName)
            return;

        UpdateShearingLayer(ent, args.Solution);
    }
}
