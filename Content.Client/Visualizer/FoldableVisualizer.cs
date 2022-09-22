using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using static Content.Shared.Foldable.SharedFoldableSystem;

namespace Content.Client.Visualizer;


public sealed class FoldableVisualizer : AppearanceVisualizer
{
    [DataField("key")]
    private string _key = default!;

    [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
    public override void OnChangeData(AppearanceComponent appearance)
    {
        base.OnChangeData(appearance);

        var entManager = IoCManager.Resolve<IEntityManager>();

        if (!entManager.TryGetComponent(appearance.Owner, out SpriteComponent? sprite)) return;

        if (appearance.TryGetData(FoldedVisuals.State, out bool folded) && folded)
        {
            sprite.LayerSetState(FoldableVisualLayers.Base, $"{_key}_folded");
        }
        else
        {
            sprite.LayerSetState(FoldableVisualLayers.Base, $"{_key}");
        }
    }

    public enum FoldableVisualLayers : byte
    {
        Base,
    }
}
