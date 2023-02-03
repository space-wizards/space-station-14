using Content.Shared.Singularity;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Singularity.Visualizers;

public sealed class SingularityVisualizerSystem : VisualizerSystem<SingularityVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SingularityVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, SingularityVisualizerComponent comp, ComponentInit args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
            sprite.LayerMapReserveBlank(comp.Layer);
    }

    protected override void OnAppearanceChange(EntityUid uid, SingularityVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if(!AppearanceSystem.TryGetData(uid, SingularityVisuals.Level, out byte level, args.Component))
            return;

        args.Sprite.LayerSetSprite(comp.Layer,
            new SpriteSpecifier.Rsi(new ResourcePath("Structures/Power/Generation/Singularity/singularity_" + level + ".rsi"), "singularity_" + level));
    }
}
