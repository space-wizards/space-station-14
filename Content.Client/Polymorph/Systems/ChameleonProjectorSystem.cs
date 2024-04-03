using Content.Shared.Polymorph.Components;
using Content.Shared.Polymorph.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Client.Polymorph.Systems;

public sealed class ChameleonProjectorSystem : SharedChameleonProjectorSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISerializationManager _serMan = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChameleonDisguiseComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(Entity<ChameleonDisguiseComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!GetSprite(ent.Comp, out var src))
            return;

        // remove then re-add to prevent a funny
        RemComp<SpriteComponent>(ent);
        var dest = AddComp<SpriteComponent>(ent);
        _serMan.CopyTo(src, ref dest, notNullableOverride: true);
    }

    private bool GetSprite(ChameleonDisguiseComponent comp, [NotNullWhen(true)] out SpriteComponent? sprite)
    {
        sprite = null;
        if (TryComp(comp.SourceEntity, out sprite))
            return true;

        if (!_proto.TryIndex<EntityPrototype>(comp.SourceProto, out var proto))
            return false;

        return proto.TryGetComponent(out sprite);
    }
}
