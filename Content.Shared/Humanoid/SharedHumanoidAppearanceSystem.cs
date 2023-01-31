using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Humanoid;

/// <summary>
///     HumanoidSystem. Primarily deals with the appearance and visual data
///     of a humanoid entity. HumanoidVisualizer is what deals with actually
///     organizing the sprites and setting up the sprite component's layers.
///
///     This is a shared system, because while it is server authoritative,
///     you still need a local copy so that players can set up their
///     characters.
/// </summary>
public abstract class SharedHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;

    public const string DefaultSpecies = "Human";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidAppearanceComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(EntityUid uid, HumanoidAppearanceComponent component, ref ComponentGetState args)
    {
        args.State = new HumanoidAppearanceState(component.MarkingSet,
            component.PermanentlyHidden,
            component.HiddenLayers,
            component.CustomBaseLayers,
            component.Sex,
            component.Gender,
            component.Age,
            component.Species,
            component.SkinColor,
            component.EyeColor);
    }

    /// <summary>
    ///     Toggles a humanoid's sprite layer visibility.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="layer">Layer to toggle visibility for</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetLayerVisibility(EntityUid uid,
        HumanoidVisualLayers layer,
        bool visible,
        bool permanent = false,
        HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
            return;

        var dirty = false;
        SetLayerVisibility(uid, humanoid, layer, visible, permanent, ref dirty);
        if (dirty)
            Dirty(humanoid);
    }

    /// <summary>
    ///     Sets the visibility for multiple layers at once on a humanoid's sprite.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="layers">An enumerable of all sprite layers that are going to have their visibility set</param>
    /// <param name="visible">The visibility state of the layers given</param>
    /// <param name="permanent">If this is a permanent change, or temporary. Permanent layers are stored in their own hash set.</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetLayersVisibility(EntityUid uid, IEnumerable<HumanoidVisualLayers> layers, bool visible, bool permanent = false,
        HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
            return;

        var dirty = false;

        foreach (var layer in layers)
        {
            SetLayerVisibility(uid, humanoid, layer, visible, permanent, ref dirty);
        }

        if (dirty)
            Dirty(humanoid);
    }

    protected virtual void SetLayerVisibility(
        EntityUid uid,
        HumanoidAppearanceComponent humanoid,
        HumanoidVisualLayers layer,
        bool visible,
        bool permanent,
        ref bool dirty)
    {
        if (visible)
        {
            if (permanent)
                dirty |= humanoid.PermanentlyHidden.Remove(layer);

            dirty |= humanoid.HiddenLayers.Remove(layer);
        }
        else
        {
            if (permanent)
                dirty |= humanoid.PermanentlyHidden.Add(layer);

            dirty |= humanoid.HiddenLayers.Add(layer);
        }
    }

    /// <summary>
    ///     Set a humanoid mob's species. This will change their base sprites, as well as their current
    ///     set of markings to fit against the mob's new species.
    /// </summary>
    /// <param name="uid">The humanoid mob's UID.</param>
    /// <param name="species">The species to set the mob to. Will return if the species prototype was invalid.</param>
    /// <param name="sync">Whether to immediately synchronize this to the humanoid mob, or not.</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetSpecies(EntityUid uid, string species, bool sync = true, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid) || !_prototypeManager.TryIndex<SpeciesPrototype>(species, out var prototype))
        {
            return;
        }

        humanoid.Species = species;
        humanoid.MarkingSet.FilterSpecies(species, _markingManager);
        var oldMarkings = humanoid.MarkingSet.GetForwardEnumerator().ToList();
        humanoid.MarkingSet = new(oldMarkings, prototype.MarkingPoints, _markingManager, _prototypeManager);

        if (sync)
            Dirty(humanoid);
    }

    /// <summary>
    ///     Sets the skin color of this humanoid mob. Will only affect base layers that are not custom,
    ///     custom base layers should use <see cref="SetBaseLayerColor"/> instead.
    /// </summary>
    /// <param name="uid">The humanoid mob's UID.</param>
    /// <param name="skinColor">Skin color to set on the humanoid mob.</param>
    /// <param name="sync">Whether to synchronize this to the humanoid mob, or not.</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public virtual void SetSkinColor(EntityUid uid, Color skinColor, bool sync = true, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
            return;

        humanoid.SkinColor = skinColor;

        if (sync)
            Dirty(humanoid);
    }

    /// <summary>
    ///     Sets the base layer ID of this humanoid mob. A humanoid mob's 'base layer' is
    ///     the skin sprite that is applied to the mob's sprite upon appearance refresh.
    /// </summary>
    /// <param name="uid">The humanoid mob's UID.</param>
    /// <param name="layer">The layer to target on this humanoid mob.</param>
    /// <param name="id">The ID of the sprite to use. See <see cref="HumanoidSpeciesSpriteLayer"/>.</param>
    /// <param name="sync">Whether to synchronize this to the humanoid mob, or not.</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetBaseLayerId(EntityUid uid, HumanoidVisualLayers layer, string? id, bool sync = true,
        HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
            return;

        if (humanoid.CustomBaseLayers.TryGetValue(layer, out var info))
            humanoid.CustomBaseLayers[layer] = info with { ID = id };
        else
            humanoid.CustomBaseLayers[layer] = new(id);

        if (sync)
            Dirty(humanoid);
    }

    /// <summary>
    ///     Sets the color of this humanoid mob's base layer. See <see cref="SetBaseLayerId"/> for a
    ///     description of how base layers work.
    /// </summary>
    /// <param name="uid">The humanoid mob's UID.</param>
    /// <param name="layer">The layer to target on this humanoid mob.</param>
    /// <param name="color">The color to set this base layer to.</param>
    public void SetBaseLayerColor(EntityUid uid, HumanoidVisualLayers layer, Color? color, bool sync = true, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
            return;

        if (humanoid.CustomBaseLayers.TryGetValue(layer, out var info))
            humanoid.CustomBaseLayers[layer] = info with { Color = color };
        else
            humanoid.CustomBaseLayers[layer] = new(null, color);

        if (sync)
            Dirty(humanoid);
    }

    /// <summary>
    ///     Set a humanoid mob's sex. This will not change their gender.
    /// </summary>
    /// <param name="uid">The humanoid mob's UID.</param>
    /// <param name="sex">The sex to set the mob to.</param>
    /// <param name="sync">Whether to immediately synchronize this to the humanoid mob, or not.</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetSex(EntityUid uid, Sex sex, bool sync = true, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid) || humanoid.Sex == sex)
            return;

        var oldSex = humanoid.Sex;
        humanoid.Sex = sex;
        RaiseLocalEvent(uid, new SexChangedEvent(oldSex, sex));

        if (sync)
        {
            Dirty(humanoid);
        }
    }
}
