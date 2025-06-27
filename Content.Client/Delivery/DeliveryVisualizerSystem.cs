using Content.Shared.Delivery;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Delivery;

public sealed class DeliveryVisualizerSystem : VisualizerSystem<DeliveryComponent>
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private static readonly ProtoId<JobIconPrototype> UnknownIcon = "JobIconUnknown";

    protected override void OnAppearanceChange(EntityUid uid, DeliveryComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        _appearance.TryGetData(uid, DeliveryVisuals.JobIcon, out string job, args.Component);

        if (string.IsNullOrEmpty(job))
            job = UnknownIcon;

        if (!_prototype.TryIndex<JobIconPrototype>(job, out var icon))
        {
            SpriteSystem.LayerSetTexture((uid, args.Sprite), DeliveryVisualLayers.JobStamp, SpriteSystem.Frame0(_prototype.Index("JobIconUnknown")));
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

