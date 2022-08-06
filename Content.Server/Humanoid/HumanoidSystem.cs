using System.Linq;
using Content.Shared.CharacterAppearance;
using Content.Shared.Humanoid;

namespace Content.Server.Humanoid;

public sealed class HumanoidSystem : SharedHumanoidSystem
{
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
            component.CurrentMarkings != null
                ? component.CurrentMarkings.GetForwardEnumerator().ToList()
                : new());
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
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        // TODO: Add marking
    }

    public void RemoveMarking(EntityUid uid, string marking, Color? color = null, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        // TODO: Remove marking
    }
}
