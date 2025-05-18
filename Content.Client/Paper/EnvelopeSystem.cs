using Content.Shared.Paper;
using Robust.Client.GameObjects;

namespace Content.Client.Paper;

public sealed class EnvelopeSystem : VisualizerSystem<EnvelopeComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

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

        _sprite.LayerSetVisible((ent.Owner, sprite), EnvelopeVisualLayers.Open, ent.Comp.State == EnvelopeComponent.EnvelopeState.Open);
        _sprite.LayerSetVisible((ent.Owner, sprite), EnvelopeVisualLayers.Sealed, ent.Comp.State == EnvelopeComponent.EnvelopeState.Sealed);
        _sprite.LayerSetVisible((ent.Owner, sprite), EnvelopeVisualLayers.Torn, ent.Comp.State == EnvelopeComponent.EnvelopeState.Torn);
    }

    public enum EnvelopeVisualLayers : byte
    {
        Open,
        Sealed,
        Torn
    }
}
