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

/// <inheritdoc cref="ShearableComponent"/>
public sealed partial class SharedShearableSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShearableComponent, GetVerbsEvent<AlternativeVerb>>(AddShearVerb);
        SubscribeLocalEvent<ShearableComponent, InteractUsingEvent>(OnInteractUsingEvent);
        SubscribeLocalEvent<ShearableComponent, ExaminedEvent>(Examined);
        SubscribeLocalEvent<ShearableComponent, ShearingDoAfterEvent>(OnSheared);
        SubscribeLocalEvent<ShearableComponent, SolutionChangedEvent>(OnSolutionChange);
    }

    /// <summary>
    ///     Checks if the target entity can currently be sheared.
    /// </summary>
    /// <param name="ent">The shearable entity that will be checked, and the ShearableComponent combined with it.</param>
    /// <param name="shearingSolutionToRemove">An out variable of the reagent that will be removed from the target entity if it is sheared.</param>
    /// <param name="usedItem">The held item that is being used to shear the target entity.</param>
    /// <param name="checkItem">If false then skip checking for the correct shearing tool.</param>
    /// <returns>
    ///     A <see langword="bool"/>, true means the entity can be sheared, false means it cannot.
    /// </returns>
    public bool CanShear(Entity<ShearableComponent> ent, [NotNullWhen(true)] out FixedPoint2? shearingSolutionToRemove, EntityUid? usedItem = null, bool checkItem = true)
    {
        // Set these to null in-case we return early.
        shearingSolutionToRemove = null;

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
        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.TargetSolutionName, ref ent.Comp.ShearingSolutionEnt, out var shearingSolutionState))
        {
            return false;
        }

        // This is the amount of shearing solution inside the entity. e.g. wool/fibre in the sheep.
        var targetSolutionQuantity = shearingSolutionState.Volume;

        // Work out the number of products to spawn.
        var productsToSpawn = targetSolutionQuantity / ent.Comp.SolutionPerProduct;
        if (productsToSpawn < 1)
        {
            // Nothing to spawn so give up now.
            shearingSolutionToRemove = 0;
            return false;
        }
        else if (ent.Comp.MaximumProductsSpawned is not null && ent.Comp.MaximumProductsSpawned < productsToSpawn)
        {
            // Truncate to maximum stack.
            productsToSpawn = (int)ent.Comp.MaximumProductsSpawned;
        }

        // Force into an int to truncate any remainder, because we can only spawn whole items.
        shearingSolutionToRemove = productsToSpawn.Int() * ent.Comp.SolutionPerProduct;

        // Fail if the shearable creature has no targetSolutionName to be sheared.
        if (shearingSolutionToRemove <= 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Override function that doesn't include the shearingSolutionToRemove out var.
    /// </summary>
    /// <seealso cref="CanShear(Entity{ShearableComponent}, out FixedPoint2?, EntityUid?, bool)"/>
    public bool CanShear(Entity<ShearableComponent> ent, EntityUid? usedItem = null, bool checkItem = true)
    {
        return CanShear(ent, out _, usedItem, checkItem);
    }

    /// <summary>
    ///     Handles shearing when the player left-clicks an entity.
    ///     Doesn't run any checks, those are handled by <see cref="CanShear(Entity{ShearableComponent}, out FixedPoint2?, EntityUid?, bool)"/>.
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
        if (!CanShear(ent, out var shearingSolutionToRemove, usedItem: toolUsed, true))
        {
            // Failed, if the entity has no solution then show a popup.
            if (shearingSolutionToRemove <= 0)
            {
                var shearedProduct = ProtoMan.Index(ent.Comp.ShearedProductId);
                var feedbackPopupString = Loc.GetString("shearable-system-no-product",
                    ("target", Identity.Entity(ent.Owner, EntityManager)),
                    ("product", shearedProduct.Name));
                _popup.PopupClient(feedbackPopupString, ent.Owner, userUid);
            }

            // Fail
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
        if (!CanShear(ent, out var shearingSolutionToRemove, null, false))
        {
            return;
        }

        // Check ShearingSolutionEnt has resolved for sure.
        if (ent.Comp.ShearingSolutionEnt is null)
        {
            return;
        }

        // Lookup some variables we need.
        var shearedProduct = ProtoMan.Index(ent.Comp.ShearedProductId);

        // Mark as handled so we don't duplicate.
        args.Handled = true;

        // Failure message, if the shearable creature has no targetSolutionName to be sheared.
        if (shearingSolutionToRemove <= 0)
        {
            var feedbackPopupString = Loc.GetString("shearable-system-no-product",
                ("target", Identity.Entity(ent.Owner, EntityManager)),
                ("product", shearedProduct.Name));
            _popup.PopupClient(feedbackPopupString, ent.Owner, args.Args.User);
            return;
        }

        // Split the solution inside the creature by solutionToRemove, return what was removed.
        var removedSolution = _solutionContainer.SplitSolution(ent.Comp.ShearingSolutionEnt.Value, (FixedPoint2)shearingSolutionToRemove);

        // Psuedo shared randomness
        // Can be replaced with SharedRandom once #5849 is merged.

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var center = ent.Owner.ToCoordinates();
        // Spawn product.
        for (var i = 0; i < removedSolution.Volume / ent.Comp.SolutionPerProduct; i++)
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
    /// <param name="ent">the entity containing a shearable component that will be checked.</param>
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
            // Default to empty string, if we just can't resolve the tool quality for whatever reason localisation has a blank variable.
            var toolQuality = string.Empty;
            var toolQualityProto = ProtoMan.Index(ent.Comp.ToolQuality);
            // If a ToolQuality has been specified set its name to toolQuality so it appears in localisation.
            if (toolQualityProto is not null)
            {
                // Tool quality names are a Loc string so look up that.
                // If a Loc string isn't found then it will just return the same ID, which means it hasn't been configured right so just error and return.
                if (!Loc.TryGetString(toolQualityProto.Name, out toolQuality))
                {
                    Log.Warning($"Tried to lookup examine text for a shearable entity \"{Name(ent.Owner)}\" but the configured toolQuality ({toolQualityProto.ID}) name: \"{toolQualityProto.Name}\" is not a Loc string.");
                    return;
                }
            }
            // ALL SYSTEMS GO!
            args.PushMarkup(Loc.GetString(ent.Comp.ShearableMarkupText.Value, ("target", Identity.Entity(ent.Owner, EntityManager)), ("toolQuality", toolQuality.ToLower())));
            return;
        }

        if (ent.Comp.UnShearableMarkupText == null || !(shearingSolutionToRemove <= 0))
            return;

        args.PushMarkup(Loc.GetString(ent.Comp.UnShearableMarkupText.Value, ("target", Identity.Entity(ent.Owner, EntityManager))));
    }

    /// <summary>
    ///     This function changes the animal's shearable layer based on the solution volume.
    ///     e.g. when a sheep's wool solution volume drops below 2.5, which is the minimum needed to shear it, the wool will disappear.
    /// </summary>
    /// <param name="ent">the entity containing a shearable component that will be checked.</param>
    /// <param name="sol">a SolutionContainerChangedEvent object passed by the OnSolutionChange event.</param>
    private void UpdateShearingLayer(Entity<ShearableComponent> ent, Solution sol)
    {
        // If solution is less than the minimum then disable the shearable layer.
        if (sol.Volume < ent.Comp.SolutionPerProduct)
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
    ///     Listens for changes in solution, checks if it's a shearable solution, and passes it to UpdateShearingLayer.
    ///     Depending on the result, the shearable layer may change.
    /// </summary>
    /// <param name="ent">the entity containing a shearable component that will be checked.</param>
    /// <param name="args">Arguments passed through by the ExaminedEvent.</param>
    private void OnSolutionChange(Entity<ShearableComponent> ent, ref SolutionChangedEvent args)
    {
        // The changes are already networked as part of the same game state.
        if (_timing.ApplyingState)
            return;

        // Only interested in shearable solution, ignore the rest.
        if (args.Solution.Comp.Id != ent.Comp.TargetSolutionName)
            return;

        UpdateShearingLayer(ent, args.Solution.Comp.Solution);
    }
}
