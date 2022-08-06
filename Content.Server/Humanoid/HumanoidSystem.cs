using System.Linq;
using Content.Shared.CharacterAppearance;
using Content.Shared.Humanoid;
using Content.Shared.Markings;

namespace Content.Server.Humanoid;

public sealed class HumanoidSystem : SharedHumanoidSystem
{
    [Dependency] private readonly MarkingManager _markingManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HumanoidComponent, ComponentInit>(OnInit);
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
        if (!string.IsNullOrEmpty(humanoid.Species))
        {
            Synchronize(uid, humanoid);
        }
    }

    public void SetSkinColor(EntityUid uid, Color skinColor, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        humanoid.SkinColor = skinColor;
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

    public void AddMarking(EntityUid uid, string marking, Color? color = null, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings().TryGetValue(marking, out var prototype))
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
    }

    /// <summary>
    ///     Remove a marking by ID. This will attempt to fetch
    ///     the marking, removing it if possible.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="marking"></param>
    /// <param name="humanoid"></param>
    public void RemoveMarking(EntityUid uid, string marking, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings().TryGetValue(marking, out var prototype))
        {
            return;
        }

        humanoid.CurrentMarkings.Remove(prototype.MarkingCategory, marking);
    }
}
