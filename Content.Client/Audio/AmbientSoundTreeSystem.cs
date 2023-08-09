using Content.Shared.Audio;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client.Audio;

public sealed class AmbientSoundTreeSystem : ComponentTreeSystem<AmbientSoundTreeComponent, AmbientSoundComponent>
{
    #region Component Tree Overrides
    protected override bool DoFrameUpdate => false;
    protected override bool DoTickUpdate => true;
    protected override int InitialCapacity => 256;
    protected override bool Recursive => true;

    protected override Box2 ExtractAabb(in ComponentTreeEntry<AmbientSoundComponent> entry, Vector2 pos, Angle rot)
        => new Box2(pos - entry.Component.Range, pos + entry.Component.Range);

    protected override Box2 ExtractAabb(in ComponentTreeEntry<AmbientSoundComponent> entry)
    {
        if (entry.Component.TreeUid == null)
            return default;

        var pos = XformSystem.GetRelativePosition(
            entry.Transform,
            entry.Component.TreeUid.Value,
            GetEntityQuery<TransformComponent>());

        return ExtractAabb(in entry, pos, default);
    }
    #endregion
}
