using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Stains;

public abstract class SharedStainsSystem : EntitySystem
{
}

/// <summary>
/// Do after event for squeezing clothes, removing the solution from <see cref="StainableComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SqueezeDoAfterEvent : SimpleDoAfterEvent
{
}
