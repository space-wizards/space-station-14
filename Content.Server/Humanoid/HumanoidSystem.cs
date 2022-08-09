using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.CharacterAppearance;
using Content.Shared.Humanoid;
using Content.Shared.Markings;
using Content.Shared.Species;
using Robust.Shared.Prototypes;

namespace Content.Server.Humanoid;

public sealed class HumanoidSystem : SharedHumanoidSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HumanoidComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HumanoidComponent, PlayerSpawnCompleteEvent>(OnSpawnComplete);
    }

    private void Synchronize(EntityUid uid, HumanoidComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        SetAppearance(uid,
            component.Species,
            component.CustomBaseLayers,
            component.SkinColor,
            component.HiddenLayers.ToList(),
            component.CurrentMarkings.GetForwardEnumerator().ToList());
    }

    private void OnInit(EntityUid uid, HumanoidComponent humanoid, ComponentInit args)
    {
        if (string.IsNullOrEmpty(humanoid.Species))
        {
            // this is an invalid state
            return;
        }

        if (!string.IsNullOrEmpty(humanoid.Initial)
            && _prototypeManager.TryIndex(humanoid.Initial, out HumanoidMarkingStartingSet? startingSet))
        {
            foreach (var marking in startingSet.Markings)
            {
                AddMarking(uid, marking.MarkingId, marking.MarkingColors, false);
            }

            foreach (var (layer, id) in startingSet.CustomBaseLayers)
            {
                humanoid.CustomBaseLayers.Add(layer, new SharedHumanoidComponent.CustomBaseLayerInfo(id, humanoid.SkinColor));
            }
        }

        EnsureDefaultMarkings(uid, humanoid);

        Synchronize(uid, humanoid);
    }

    private void OnSpawnComplete(EntityUid uid, HumanoidComponent humanoid, PlayerSpawnCompleteEvent args)
    {
        humanoid.Species = args.Profile.Species;
        humanoid.Sex = args.Profile.Sex;

        SetSkinColor(uid, args.Profile.Appearance.SkinColor, false);

        // Hair/facial hair - this may eventually be deprecated.

        AddMarking(uid, args.Profile.Appearance.HairStyleId, args.Profile.Appearance.HairColor, false);
        AddMarking(uid, args.Profile.Appearance.FacialHairStyleId, args.Profile.Appearance.FacialHairColor, false);

        foreach (var marking in args.Profile.Appearance.Markings)
        {
            AddMarking(uid, marking.MarkingId, marking.MarkingColors, false);
        }

        EnsureDefaultMarkings(uid, humanoid);

        Synchronize(uid);
    }

    public void SetSkinColor(EntityUid uid, Color skinColor, bool sync = true, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        humanoid.SkinColor = skinColor;

        if (sync)
            Synchronize(uid, humanoid);
    }

    public void ToggleHiddenLayer(EntityUid uid, HumanoidVisualLayers layer, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        if (humanoid.HiddenLayers.Contains(layer))
        {
            humanoid.HiddenLayers.Remove(layer);
        }
        else
        {
            humanoid.HiddenLayers.Add(layer);
        }

        Synchronize(uid, humanoid);
    }

    public void AddMarking(EntityUid uid, string marking, Color? color = null, bool sync = true, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
        {
            return;
        }

        var markingObject = prototype.AsMarking();
        if (color != null)
        {
            for (var i = 0; i < prototype.Sprites.Count; i++)
            {
                markingObject.SetColor(i, color.Value);
            }
        }

        humanoid.CurrentMarkings.AddBack(prototype.MarkingCategory, markingObject);

        if (sync)
            Synchronize(uid, humanoid);
    }

    public void AddMarking(EntityUid uid, string marking, IReadOnlyList<Color> colors, bool sync = true, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
        {
            return;
        }

        humanoid.CurrentMarkings.AddBack(prototype.MarkingCategory, new Marking(marking, colors));

        if (sync)
            Synchronize(uid, humanoid);
    }

    /// <summary>
    ///     Remove a marking by ID. This will attempt to fetch
    ///     the marking, removing it if possible.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="marking"></param>
    /// <param name="humanoid"></param>
    public void RemoveMarking(EntityUid uid, string marking, bool sync = true, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
        {
            return;
        }

        humanoid.CurrentMarkings.Remove(prototype.MarkingCategory, marking);

        if (sync)
            Synchronize(uid, humanoid);
    }

    private void EnsureDefaultMarkings(EntityUid uid, HumanoidComponent? humanoid)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        humanoid.CurrentMarkings.EnsureDefault(humanoid.SkinColor, _markingManager);
    }
}
