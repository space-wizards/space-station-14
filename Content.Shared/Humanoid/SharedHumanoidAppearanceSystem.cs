using System.IO;
using System.Linq;
using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Decals;
using Content.Shared.Examine;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

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
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISerializationManager _serManager = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;
    [Dependency] private readonly GrammarSystem _grammarSystem = default!;

    [ValidatePrototypeId<SpeciesPrototype>]
    public const string DefaultSpecies = "Human";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidAppearanceComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HumanoidAppearanceComponent, ExaminedEvent>(OnExamined);
    }

    public DataNode ToDataNode(HumanoidCharacterProfile profile)
    {
        var export = new HumanoidProfileExport()
        {
            ForkId = _cfgManager.GetCVar(CVars.BuildForkId),
            Profile = profile,
        };

        var dataNode = _serManager.WriteValue(export, alwaysWrite: true, notNullableOverride: true);
        return dataNode;
    }

    public HumanoidCharacterProfile FromStream(Stream stream, ICommonSession session)
    {
        using var reader = new StreamReader(stream, EncodingHelpers.UTF8);
        var yamlStream = new YamlStream();
        yamlStream.Load(reader);

        var root = yamlStream.Documents[0].RootNode;
        var export = _serManager.Read<HumanoidProfileExport>(root.ToDataNode(), notNullableOverride: true);

        /*
         * Add custom handling here for forks / version numbers if you care.
         */

        var profile = export.Profile;
        var collection = IoCManager.Instance;
        profile.EnsureValid(session, collection!);
        return profile;
    }

    private void OnInit(EntityUid uid, HumanoidAppearanceComponent humanoid, ComponentInit args)
    {
        if (string.IsNullOrEmpty(humanoid.Species) || _netManager.IsClient && !IsClientSide(uid))
        {
            return;
        }

        if (string.IsNullOrEmpty(humanoid.Initial)
            || !_proto.TryIndex(humanoid.Initial, out HumanoidProfilePrototype? startingSet))
        {
            LoadProfile(uid, HumanoidCharacterProfile.DefaultWithSpecies(humanoid.Species), humanoid);
            return;
        }

        // Do this first, because profiles currently do not support custom base layers
        foreach (var (layer, info) in startingSet.CustomBaseLayers)
        {
            humanoid.CustomBaseLayers.Add(layer, info);
        }

        LoadProfile(uid, startingSet.Profile, humanoid);
    }

    private void OnExamined(EntityUid uid, HumanoidAppearanceComponent component, ExaminedEvent args)
    {
        var identity = Identity.Entity(uid, EntityManager);
        var species = GetSpeciesRepresentation(component.Species).ToLower();
        var age = GetAgeRepresentation(component.Species, component.Age);

        args.PushText(Loc.GetString("humanoid-appearance-component-examine", ("user", identity), ("age", age), ("species", species)));
    }

    /// <summary>
    ///     Toggles a humanoid's sprite layer visibility.
    /// </summary>
    /// <param name="ent">Humanoid entity</param>
    /// <param name="layer">Layer to toggle visibility for</param>
    /// <param name="visible">Whether to hide or show the layer. If more than once piece of clothing is hiding the layer, it may remain hidden.</param>
    /// <param name="source">Equipment slot that has the clothing that is (or was) hiding the layer. If not specified, the change is "permanent" (i.e., see <see cref="HumanoidAppearanceComponent.PermanentlyHidden"/>)</param>
    public void SetLayerVisibility(Entity<HumanoidAppearanceComponent?> ent,
        HumanoidVisualLayers layer,
        bool visible,
        SlotFlags? source = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        var dirty = false;
        SetLayerVisibility(ent!, layer, visible, source, ref dirty);
        if (dirty)
            Dirty(ent);
    }

    /// <summary>
    ///     Clones a humanoid's appearance to a target mob, provided they both have humanoid components.
    /// </summary>
    /// <param name="source">Source entity to fetch the original appearance from.</param>
    /// <param name="target">Target entity to apply the source entity's appearance to.</param>
    /// <param name="sourceHumanoid">Source entity's humanoid component.</param>
    /// <param name="targetHumanoid">Target entity's humanoid component.</param>
    public void CloneAppearance(EntityUid source, EntityUid target, HumanoidAppearanceComponent? sourceHumanoid = null,
        HumanoidAppearanceComponent? targetHumanoid = null)
    {
        if (!Resolve(source, ref sourceHumanoid) || !Resolve(target, ref targetHumanoid))
            return;

        targetHumanoid.Species = sourceHumanoid.Species;
        targetHumanoid.SkinColor = sourceHumanoid.SkinColor;
        targetHumanoid.EyeColor = sourceHumanoid.EyeColor;
        targetHumanoid.Age = sourceHumanoid.Age;
        SetSex(target, sourceHumanoid.Sex, false, targetHumanoid);
        targetHumanoid.CustomBaseLayers = new(sourceHumanoid.CustomBaseLayers);
        targetHumanoid.MarkingSet = new(sourceHumanoid.MarkingSet);

        targetHumanoid.Gender = sourceHumanoid.Gender;

        if (TryComp<GrammarComponent>(target, out var grammar))
            _grammarSystem.SetGender((target, grammar), sourceHumanoid.Gender);

        Dirty(target, targetHumanoid);
    }

    /// <summary>
    ///     Sets the visibility for multiple layers at once on a humanoid's sprite.
    /// </summary>
    /// <param name="ent">Humanoid entity</param>
    /// <param name="layers">An enumerable of all sprite layers that are going to have their visibility set</param>
    /// <param name="visible">The visibility state of the layers given</param>
    public void SetLayersVisibility(Entity<HumanoidAppearanceComponent?> ent,
        IEnumerable<HumanoidVisualLayers> layers,
        bool visible)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        var dirty = false;

        foreach (var layer in layers)
        {
            SetLayerVisibility(ent!, layer, visible, null, ref dirty);
        }

        if (dirty)
            Dirty(ent);
    }

    /// <inheritdoc cref="SetLayerVisibility(Entity{HumanoidAppearanceComponent?},HumanoidVisualLayers,bool,Nullable{SlotFlags})"/>
    public virtual void SetLayerVisibility(
        Entity<HumanoidAppearanceComponent> ent,
        HumanoidVisualLayers layer,
        bool visible,
        SlotFlags? source,
        ref bool dirty)
    {
#if DEBUG
        if (source is {} s)
        {
            DebugTools.AssertNotEqual(s, SlotFlags.NONE);
            // Check that only a single bit in the bitflag is set
            var powerOfTwo = BitOperations.RoundUpToPowerOf2((uint)s);
            DebugTools.AssertEqual((uint)s, powerOfTwo);
        }
#endif

        if (visible)
        {
            if (source is not {} slot)
            {
                dirty |= ent.Comp.PermanentlyHidden.Remove(layer);
            }
            else if (ent.Comp.HiddenLayers.TryGetValue(layer, out var oldSlots))
            {
                // This layer might be getting hidden by more than one piece of equipped clothing.
                // remove slot flag from the set of slots hiding this layer, then check if there are any left.
                ent.Comp.HiddenLayers[layer] = ~slot & oldSlots;
                if (ent.Comp.HiddenLayers[layer] == SlotFlags.NONE)
                    ent.Comp.HiddenLayers.Remove(layer);

                dirty |= (oldSlots & slot) != 0;
            }
        }
        else
        {
            if (source is not { } slot)
            {
                dirty |= ent.Comp.PermanentlyHidden.Add(layer);
            }
            else
            {
                var oldSlots = ent.Comp.HiddenLayers.GetValueOrDefault(layer);
                ent.Comp.HiddenLayers[layer] = slot | oldSlots;
                dirty |= (oldSlots & slot) != slot;
            }

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
        if (!Resolve(uid, ref humanoid) || !_proto.TryIndex<SpeciesPrototype>(species, out var prototype))
        {
            return;
        }

        humanoid.Species = species;
        humanoid.MarkingSet.EnsureSpecies(species, humanoid.SkinColor, _markingManager);
        var oldMarkings = humanoid.MarkingSet.GetForwardEnumerator().ToList();
        humanoid.MarkingSet = new(oldMarkings, prototype.MarkingPoints, _markingManager, _proto);

        if (sync)
            Dirty(uid, humanoid);
    }

    /// <summary>
    ///     Sets the skin color of this humanoid mob. Will only affect base layers that are not custom,
    ///     custom base layers should use <see cref="SetBaseLayerColor"/> instead.
    /// </summary>
    /// <param name="uid">The humanoid mob's UID.</param>
    /// <param name="skinColor">Skin color to set on the humanoid mob.</param>
    /// <param name="sync">Whether to synchronize this to the humanoid mob, or not.</param>
    /// <param name="verify">Whether to verify the skin color can be set on this humanoid or not</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public virtual void SetSkinColor(EntityUid uid, Color skinColor, bool sync = true, bool verify = true, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
            return;

        if (!_proto.TryIndex<SpeciesPrototype>(humanoid.Species, out var species))
        {
            return;
        }

        if (verify && !SkinColor.VerifySkinColor(species.SkinColoration, skinColor))
        {
            skinColor = SkinColor.ValidSkinTone(species.SkinColoration, skinColor);
        }

        humanoid.SkinColor = skinColor;

        if (sync)
            Dirty(uid, humanoid);
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
            humanoid.CustomBaseLayers[layer] = info with { Id = id };
        else
            humanoid.CustomBaseLayers[layer] = new(id);

        if (sync)
            Dirty(uid, humanoid);
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
            Dirty(uid, humanoid);
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
        humanoid.MarkingSet.EnsureSexes(sex, _markingManager);
        RaiseLocalEvent(uid, new SexChangedEvent(oldSex, sex));

        if (sync)
        {
            Dirty(uid, humanoid);
        }
    }

    /// <summary>
    ///     Loads a humanoid character profile directly onto this humanoid mob.
    /// </summary>
    /// <param name="uid">The mob's entity UID.</param>
    /// <param name="profile">The character profile to load.</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public virtual void LoadProfile(EntityUid uid, HumanoidCharacterProfile? profile, HumanoidAppearanceComponent? humanoid = null)
    {
        if (profile == null)
            return;

        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        SetSpecies(uid, profile.Species, false, humanoid);
        SetSex(uid, profile.Sex, false, humanoid);
        humanoid.EyeColor = profile.Appearance.EyeColor;

        SetSkinColor(uid, profile.Appearance.SkinColor, false);

        humanoid.MarkingSet.Clear();

        // Add markings that doesn't need coloring. We store them until we add all other markings that doesn't need it.
        var markingFColored = new Dictionary<Marking, MarkingPrototype>();
        foreach (var marking in profile.Appearance.Markings)
        {
            if (_markingManager.TryGetMarking(marking, out var prototype))
            {
                if (!prototype.ForcedColoring)
                {
                    AddMarking(uid, marking.MarkingId, marking.MarkingColors, false);
                }
                else
                {
                    markingFColored.Add(marking, prototype);
                }
            }
        }

        // Hair/facial hair - this may eventually be deprecated.
        // We need to ensure hair before applying it or coloring can try depend on markings that can be invalid
        var hairColor = _markingManager.MustMatchSkin(profile.Species, HumanoidVisualLayers.Hair, out var hairAlpha, _proto)
            ? profile.Appearance.SkinColor.WithAlpha(hairAlpha) : profile.Appearance.HairColor;
        var facialHairColor = _markingManager.MustMatchSkin(profile.Species, HumanoidVisualLayers.FacialHair, out var facialHairAlpha, _proto)
            ? profile.Appearance.SkinColor.WithAlpha(facialHairAlpha) : profile.Appearance.FacialHairColor;

        if (_markingManager.Markings.TryGetValue(profile.Appearance.HairStyleId, out var hairPrototype) &&
            _markingManager.CanBeApplied(profile.Species, profile.Sex, hairPrototype, _proto))
        {
            AddMarking(uid, profile.Appearance.HairStyleId, hairColor, false);
        }

        if (_markingManager.Markings.TryGetValue(profile.Appearance.FacialHairStyleId, out var facialHairPrototype) &&
            _markingManager.CanBeApplied(profile.Species, profile.Sex, facialHairPrototype, _proto))
        {
            AddMarking(uid, profile.Appearance.FacialHairStyleId, facialHairColor, false);
        }

        humanoid.MarkingSet.EnsureSpecies(profile.Species, profile.Appearance.SkinColor, _markingManager, _proto);

        // Finally adding marking with forced colors
        foreach (var (marking, prototype) in markingFColored)
        {
            var markingColors = MarkingColoring.GetMarkingLayerColors(
                prototype,
                profile.Appearance.SkinColor,
                profile.Appearance.EyeColor,
                humanoid.MarkingSet
            );
            AddMarking(uid, marking.MarkingId, markingColors, false);
        }

        EnsureDefaultMarkings(uid, humanoid);

        humanoid.Gender = profile.Gender;
        if (TryComp<GrammarComponent>(uid, out var grammar))
        {
            _grammarSystem.SetGender((uid, grammar), profile.Gender);
        }

        humanoid.Age = profile.Age;

        Dirty(uid, humanoid);
    }

    /// <summary>
    ///     Adds a marking to this humanoid.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="marking">Marking ID to use</param>
    /// <param name="color">Color to apply to all marking layers of this marking</param>
    /// <param name="sync">Whether to immediately sync this marking or not</param>
    /// <param name="forced">If this marking was forced (ignores marking points)</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void AddMarking(EntityUid uid, string marking, Color? color = null, bool sync = true, bool forced = false, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
        {
            return;
        }

        var markingObject = prototype.AsMarking();
        markingObject.Forced = forced;
        if (color != null)
        {
            for (var i = 0; i < prototype.Sprites.Count; i++)
            {
                markingObject.SetColor(i, color.Value);
            }
        }

        humanoid.MarkingSet.AddBack(prototype.MarkingCategory, markingObject);

        if (sync)
            Dirty(uid, humanoid);
    }

    private void EnsureDefaultMarkings(EntityUid uid, HumanoidAppearanceComponent? humanoid)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }
        humanoid.MarkingSet.EnsureDefault(humanoid.SkinColor, humanoid.EyeColor, _markingManager);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="marking">Marking ID to use</param>
    /// <param name="colors">Colors to apply against this marking's set of sprites.</param>
    /// <param name="sync">Whether to immediately sync this marking or not</param>
    /// <param name="forced">If this marking was forced (ignores marking points)</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void AddMarking(EntityUid uid, string marking, IReadOnlyList<Color> colors, bool sync = true, bool forced = false, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
        {
            return;
        }

        var markingObject = new Marking(marking, colors);
        markingObject.Forced = forced;
        humanoid.MarkingSet.AddBack(prototype.MarkingCategory, markingObject);

        if (sync)
            Dirty(uid, humanoid);
    }

    /// <summary>
    /// Takes ID of the species prototype, returns UI-friendly name of the species.
    /// </summary>
    public string GetSpeciesRepresentation(string speciesId)
    {
        if (_proto.TryIndex<SpeciesPrototype>(speciesId, out var species))
        {
            return Loc.GetString(species.Name);
        }

        Log.Error("Tried to get representation of unknown species: {speciesId}");
        return Loc.GetString("humanoid-appearance-component-unknown-species");
    }

    public string GetAgeRepresentation(string species, int age)
    {
        if (!_proto.TryIndex<SpeciesPrototype>(species, out var speciesPrototype))
        {
            Log.Error("Tried to get age representation of species that couldn't be indexed: " + species);
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.YoungAge)
        {
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.OldAge)
        {
            return Loc.GetString("identity-age-middle-aged");
        }

        return Loc.GetString("identity-age-old");
    }
}
