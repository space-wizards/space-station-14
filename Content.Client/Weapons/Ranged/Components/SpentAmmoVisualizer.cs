using Content.Shared.Weapons.Ranged;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Weapons.Ranged.Components;

[UsedImplicitly]
public sealed class SpentAmmoVisualizer : AppearanceVisualizer
{
    public override void OnChangeData(AppearanceComponent component)
    {
        base.OnChangeData(component);
        var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);

        if (!component.TryGetData(SharedGunSystem.AmmoVisuals.Spent, out bool spent))
        {
            return;
        }

        sprite.LayerSetState(AmmoVisualLayers.Base, spent ? "spent" : "base");
    }
}

public enum AmmoVisualLayers : byte
{
    Base,
}
