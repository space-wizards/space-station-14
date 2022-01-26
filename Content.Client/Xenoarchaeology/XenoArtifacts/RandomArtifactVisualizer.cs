using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Xenoarchaeology.XenoArtifacts;

public class RandomArtifactVisualizer : AppearanceVisualizer
{
    public override void OnChangeData(AppearanceComponent component)
    {
        base.OnChangeData(component);

        var entities = IoCManager.Resolve<IEntityManager>();
        if (!entities.TryGetComponent(component.Owner, out ISpriteComponent? sprite)) return;

        if (!component.TryGetData(SharedArtifactsVisuals.SpriteIndex, out int spriteIndex))
            return;

        if (!component.TryGetData(SharedArtifactsVisuals.IsActivated, out bool isActivated))
            isActivated = false;

        var spriteIndexStr = spriteIndex.ToString("D2");
        var spritePrefix = isActivated ? "_on" : "";

        var spriteState = "ano" + spriteIndexStr + spritePrefix;
        sprite.LayerSetState(0, spriteState);
    }
}
