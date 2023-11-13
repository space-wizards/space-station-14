using Content.Shared.Sprite;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Client.Sprite;

public sealed class CopySpriteSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISerializationManager _serMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CopySpriteEvent>(OnCopySprite);
    }

    private void OnCopySprite(ref CopySpriteEvent args)
    {
        if (!_proto.TryIndex<EntityPrototype>(args.Prototype, out var proto))
            return;

        var uid = GetEntity(args.Entity);
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!proto.Components.TryGetComponent("Sprite", out var protoSprite))
            return;

        _serMan.CopyTo(protoSprite, ref sprite);
        Dirty(uid, sprite);
    }
}
