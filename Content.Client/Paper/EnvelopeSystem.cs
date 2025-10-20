using Content.Shared.Paper;
using Robust.Client.GameObjects;

namespace Content.Client.Paper;

public sealed class EnvelopeSystem : VisualizerSystem<EnvelopeComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EnvelopeComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<EnvelopeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<EnvelopeComponent> ent, SpriteComponent? sprite = null)
    {
        if (!Resolve(ent.Owner, ref sprite))
            return;

        SpriteSystem.LayerSetVisible((ent.Owner, sprite), EnvelopeVisualLayers.Open, ent.Comp.State == EnvelopeComponent.EnvelopeState.Open);
        SpriteSystem.LayerSetVisible((ent.Owner, sprite), EnvelopeVisualLayers.Sealed, ent.Comp.State == EnvelopeComponent.EnvelopeState.Sealed);
        SpriteSystem.LayerSetVisible((ent.Owner, sprite), EnvelopeVisualLayers.Torn, ent.Comp.State == EnvelopeComponent.EnvelopeState.Torn);
    }

    public enum EnvelopeVisualLayers : byte
    {
        Open,
        Sealed,
        Torn
    }
}
