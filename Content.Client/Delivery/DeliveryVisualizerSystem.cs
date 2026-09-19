using Content.Shared.Delivery;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Delivery;

public sealed partial class DeliveryVisualizerSystem : VisualizerSystem<DeliveryComponent>
{
    private static readonly ProtoId<JobIconPrototype> UnknownIcon = "JobIconUnknown";

    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, DeliveryComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        args.TryGetData<string>(DeliveryVisuals.JobIcon, out var job);

        if (string.IsNullOrEmpty(job))
            job = UnknownIcon;

        if (!ProtoMan.TryIndex<JobIconPrototype>(job, out var icon))
        {
            SpriteSystem.LayerSetTexture((uid, args.Sprite), DeliveryVisualLayers.JobStamp, SpriteSystem.Frame0(ProtoMan.Index(UnknownIcon).Icon));
            return;
        }

        SpriteSystem.LayerSetTexture((uid, args.Sprite), DeliveryVisualLayers.JobStamp, SpriteSystem.Frame0(icon.Icon));
    }
}

public enum DeliveryVisualLayers : byte
{
    Icon,
    Lock,
    FragileStamp,
    JobStamp,
    PriorityTape,
    Breakage,
    Trash,
    Bomb,
    BombPrimed,
}

public enum DeliverySpawnerVisualLayers : byte
{
    Contents,
}

