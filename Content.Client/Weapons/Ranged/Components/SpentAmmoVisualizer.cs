using Content.Shared.Weapons.Ranged.Systems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Weapons.Ranged.Components;

[UsedImplicitly]
public sealed class SpentAmmoVisualizer : AppearanceVisualizer
{
    [DataField("state")]
    private string _state = "base";

    public override void OnChangeData(AppearanceComponent component)
    {
        base.OnChangeData(component);
        var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);

        if (!component.TryGetData(AmmoVisuals.Spent, out bool spent))
        {
            return;
        }

        sprite.LayerSetState(AmmoVisualLayers.Base, spent ? $"{_state}-spent" : _state);
    }
}

public enum AmmoVisualLayers : byte
{
    Base,
}
