using Robust.Shared.Serialization;

namespace Content.Shared.Sprite;

public abstract class SharedRandomSpriteSystem : EntitySystem {}

[Serializable, NetSerializable]
public sealed partial class RandomSpriteColorComponentState : ComponentState
{
    public Dictionary<string, (string State, Color? Color)> Selected = default!;
}
