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
using System.Diagnostics.CodeAnalysis;
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
    /// <param name="ent">The shearable entity that will be checked, and the ShearableComponent combined with it.</param>
    /// <param name="shearedProduct">An out variable of the resolved sheared product prototype.</param>
    /// <param name="shearingSolutionEnt">An out variable of the resolved sheared product solution entity.</param>
    /// <param name="shearingSolutionToRemove">An out variable of the reagent that will be removed from the target entity if it is sheared.</param>
    /// <param name="feedbackPopupString">If populated, this string can be used in a client popup to describe why the creature isn't shearable. It makes use of the shearable-system-no-product loc string.</param>
    /// <param name="usedItem">The held item that is being used to shear the target entity.</param>
    /// <param name="checkItem">If false then skip checking for the correct shearing tool.</param>
    /// <returns>
    ///     A <c>bool</c>, true means the entity can be sheared, false means it cannot.
    /// </returns>
    public bool CanShear(Entity<ShearableComponent> ent, out EntityPrototype shearedProduct, [NotNullWhen(true)] out Entity<SolutionComponent>? shearingSolutionEnt, [NotNullWhen(true)] out FixedPoint2? shearingSolutionToRemove, out string? feedbackPopupString, EntityUid? usedItem = null, bool checkItem = true)
    {
        // Set these to null in-case we return early.
        shearedProduct = _proto.Index(ent.Comp.ShearedProductId);
        shearingSolutionEnt = null;
        shearingSolutionToRemove = null;
        feedbackPopupString = null;

        // Are we checking items? Has a toolQuality been defined?
        // Even if we are checking items, if no toolQuality has been defined, then they're allowed to use anything, including an empty hand.
        if (checkItem && ent.Comp.ToolQuality is not null &&
            // If so, is the player holding anything at all, and does that item have the correct toolQuality?
            (usedItem == null || !_tool.HasQuality(usedItem.Value, ent.Comp.ToolQuality)))
        {
            return false;
        }

        // Everything below this point is just calculating whether the animal
        // has enough solution to spawn at least one item in the specified stack.
        // If so, True, otherwise False.

        // Resolves the targetSolutionName as a solution inside the shearable creature. Outputs the "solution" variable.
        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.TargetSolutionName, ref shearingSolutionEnt, out var shearingSolutionState))
        {
            return false;
        }

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
            maxProductsToSpawn = (float)ent.Comp.MaximumProductsSpawned;
        }

        // Modulus the targetSolutionQuantity so no solution is wasted if it can't be divided evenly.
        // subtract targetSolutionQuantity from the remainder.
        // Everything is divided by 100, because fixedPoint2 multiplies everything by 100.
        // Math.Min ensures that no more solution than what is needed for the maximum stack is used, shear the entity multiple times if you want the rest of the product.
        // e.g.
        // targetSolutionQuantity.Value = 2500, this is how much shearable solution the target entity contains, it's equivalent to 25 units.
        // productsPerSolution = 500, this is the YAML defined number of materials we will get from shearing. It has been converted from units to reagent above, it was originally 0.2 units.
        // maxProductsToSpawn is undefined in YAML and has been defaulted to productsPerSolution (500).
        // 2500 - 2500 % 500. 500 fits nicely into 2500 so there is no remainder of 0. This means we're removing all 2500 reagent currently.
        //
        // Next, we check the maximum number of products we want to spawn, if this is less than the total reagent available then the entity will need to be sheared multiple times to deplete its resources.
        // If there's no configured maxProductsToSpawn, then we aren't imposing a limit. In this case it defaults to productsPerSolution.
        // productsPerSolution is set to 500. Therefore, the calculation is:
        // 500 * 500 / 100 = 2500 (See how it lines up with the total reagent available).
        // If we had configured a limit, e,g 3 then it would look like this:
        // 3 * 500 / 100 = 1500, 1000 reagent less than what was available.
        // We take the smaller of two values with .Min, we don't want to remove more reagent than we're using.
        shearingSolutionToRemove = FixedPoint2.Min(
                targetSolutionQuantity.Value - targetSolutionQuantity.Value % productsPerSolution,
                maxProductsToSpawn * productsPerSolution
        ) / 100;

        // Failure message, if the shearable creature has no targetSolutionName to be sheared.
        if (shearingSolutionToRemove <= 0)
        {
            feedbackPopupString = Loc.GetString("shearable-system-no-product", ("target", Identity.Entity(ent.Owner, EntityManager)), ("product", shearedProduct.Name));
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Override function, for details see <see cref="CanShear(Entity{ShearableComponent}, out EntityPrototype, out Entity{SolutionComponent}?, out FixedPoint2?, out string?, EntityUid?, bool)"/>
    /// </summary>
    public bool CanShear(Entity<ShearableComponent> ent, out string? feedbackPopupString, EntityUid? usedItem = null, bool checkItem = true)
    {
        return CanShear(ent, out _, out _, out _, out feedbackPopupString, usedItem, checkItem);
    }

    /// <summary>
    ///     Override function, for details see <see cref="CanShear(Entity{ShearableComponent}, out EntityPrototype, out Entity{SolutionComponent}?, out FixedPoint2?, out string?, EntityUid?, bool)"/>
    /// </summary>
    public bool CanShear(Entity<ShearableComponent> ent, EntityUid? usedItem = null, bool checkItem = true)
    {
        return CanShear(ent, out _, out _, out _, out _, usedItem, checkItem);
    }
    /// <summary>
    ///     Override function, for details see <see cref="CanShear(Entity{ShearableComponent}, out EntityPrototype, out Entity{SolutionComponent}?, out FixedPoint2?, out string?, EntityUid?, bool)"/>
    /// </summary>
    public bool CanShear(Entity<ShearableComponent> ent, out FixedPoint2? shearingSolutionToRemove, bool checkItem = true)
    {
        return CanShear(ent, out _, out _, out shearingSolutionToRemove, out _, null, checkItem);
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
        if (!CanShear(ent, out var feedbackPopupString, usedItem: toolUsed))
        {
            // If this string is set then create a popup now.
            if (feedbackPopupString != null)
            {
                _popup.PopupClient(feedbackPopupString, ent.Owner, userUid);
            }
            // Fail regardless of popup.
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
        // Check the action hasn't been cancelled, or hasn't already been handled.
        if (args.Cancelled || args.Handled)
            return;

        // Check again and this time get the objects we need.
        if (!CanShear(ent, out var shearedProduct, out var shearingSolutionEnt, out var shearingSolutionToRemove, out var feedbackPopupString, null, false))
        {
            return;
        }

        // Mark as handled so we don't duplicate.
        args.Handled = true;

        // Solution is measured in units but the actual value for 1u is 1000 reagent, so multiply it by 100.
        // Then, divide by 1 because it's the reagent needed for 1 product.
        var productsPerSolution = (int)(1 / ent.Comp.ProductsPerSolution * 100);

        // Failure message, if the shearable creature has no targetSolutionName to be sheared.
        if (shearingSolutionToRemove == 0)
        {
            _popup.PopupClient(feedbackPopupString, ent.Owner, args.Args.User);
            return;
        }

        // Split the solution inside the creature by solutionToRemove, return what was removed.
        var removedSolution = _solutionContainer.SplitSolution(shearingSolutionEnt.Value, (FixedPoint2)shearingSolutionToRemove);

        // Psuedo shared randomness
        // Can be replaced with SharedRandom once #5849 is merged.

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var center = ent.Owner.ToCoordinates();
        // Spawn product.
        for (var i = 0; i < removedSolution.Volume.Value / productsPerSolution; i++)
        {
            // Offset the spawn position by e.g 0.4 pixels, so they don't all stack in one spot.
            var xoffs = rand.NextFloat(-ent.Comp.RandomSpawnOffsetVariation, ent.Comp.RandomSpawnOffsetVariation);
            var yoffs = rand.NextFloat(-ent.Comp.RandomSpawnOffsetVariation, ent.Comp.RandomSpawnOffsetVariation);
            var pos = center.Offset(new Vector2(xoffs, yoffs));

            PredictedSpawnAtPosition(ent.Comp.ShearedProductId, pos);
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

        var verbDisabled = false;
        var toolUsed = args.Using;

        // Check.
        if (!CanShear(ent, toolUsed, true))
        {
            // Still adds the verb but it's disabled.
            verbDisabled = true;
        }

        // Construct verb object.
        var user = args.User;

        AlternativeVerb verb =
            new()
            {
                Act = () => AttemptShear(ent, user, toolUsed),
                Disabled = verbDisabled,
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
        if (CanShear(ent, out var shearingSolutionToRemove, checkItem: false) && ent.Comp.ShearableMarkupText != null)
        {
            // Default to empty string, if we just can't resolve the tool quality for whatever reason localisation have a blank variable..
            var toolQuality = string.Empty;
            var toolQualityProto = _proto.Index(ent.Comp.ToolQuality);
            // If a ToolQuality has been specified set its name to toolQuality so it appears in localisation.
            if (toolQualityProto is not null)
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
            args.PushMarkup(Loc.GetString(ent.Comp.ShearableMarkupText.Value, ("target", Identity.Entity(ent.Owner, EntityManager)), ("toolQuality", toolQuality)));
            return;
        }

        if (ent.Comp.UnShearableMarkupText == null || !(shearingSolutionToRemove <= 0))
            return;

        args.PushMarkup(Loc.GetString(ent.Comp.UnShearableMarkupText.Value, ("target", Identity.Entity(ent.Owner, EntityManager))));
    }

    /// <summary>
    ///     This function changes the animal's shearable layer based on the solution volume.
    ///     e.g. when a sheep's wool solution volume drops below 5, which is the minimum needed to shear it, the wool will disapear.
    /// </summary>
    /// <param name="ent">the entity containing a wooly component that will be checked.</param>
    /// <param name="sol">a SolutionContainerChangedEvent object passed by the OnSolutionChange event.</param>
    private void UpdateShearingLayer(Entity<ShearableComponent> ent, Solution sol)
    {
        // The minimum solution required to spawn one product.
        var minimumSol = 100 / ent.Comp.ProductsPerSolution;

        // If solution is less than the minimum then disable the shearable layer.
        if (sol.Volume.Value < minimumSol)
        {
            // Remove wool layer
            _appearance.SetData(ent.Owner, ShearableVisuals.Shearable, false);
        }
        // If solution is more than the minimum then enable the shearable layer.
        else
        {
            // Add wool layer
            _appearance.SetData(ent.Owner, ShearableVisuals.Shearable, true);
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
        // The changes are already networked as part of the same game state.
        if (_timing.ApplyingState)
            return;

        // Only interested in wool solution, ignore the rest.
        if (args.SolutionId != ent.Comp.TargetSolutionName)
            return;

        //UpdateShearingLayer(ent, args.Solution);
    }
}
