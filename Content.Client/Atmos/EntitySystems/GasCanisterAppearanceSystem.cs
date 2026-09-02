using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
/// Used to change the appearance of gas canisters.
/// </summary>
public sealed partial class GasCanisterAppearanceSystem : VisualizerSystem<GasCanisterComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, GasCanisterComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<string>(uid, PaintableVisuals.Prototype, out var protoName, args.Component) || args.Sprite is not { } old)
            return;

        if (!ProtoMan.HasIndex(protoName))
            return;

        // Create the given prototype and get its first layer.
        var tempUid = Spawn(protoName);
        SpriteSystem.LayerSetRsiState(uid, 0, SpriteSystem.LayerGetRsiState(tempUid, 0));
        QueueDel(tempUid);
    }
}
