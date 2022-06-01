using Content.Shared.Weapons.Ranged.Systems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Weapons.Ranged.Components;

[UsedImplicitly]
public sealed class SpentAmmoVisualizer : AppearanceVisualizer
{
    /// <summary>
    /// Should we do "{_state}-spent" or just "spent"
    /// </summary>
    [DataField("suffix")] private bool _suffix = true;

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

        string state;

        if (spent)
            state = _suffix ? $"{_state}-spent" : "spent";
        else
            state = _state;

        sprite.LayerSetState(AmmoVisualLayers.Base, state);
    }
}

public enum AmmoVisualLayers : byte
{
    Base,
}
