using Content.Shared.Damage;
using Robust.Shared.Physics;

namespace Content.Shared.Blocking;

/// <summary>
/// This component gets dynamically added to an Entity via the <see cref="SharedBlockingSystem"/>
/// </summary>
[RegisterComponent]
public sealed class SharedBlockingUserComponent : Component
{
    /// <summary>
    /// The entity that's being used to block
    /// </summary>
    [ViewVariables]
    public EntityUid? BlockingItem;

    [ViewVariables]
    public DamageModifierSet Modifiers = default!;

    /// <summary>
    /// Stores the entities original bodytype
    /// Used so that it can be put back to what it was after anchoring
    /// </summary>
    [ViewVariables]
    public BodyType OriginalBodyType;

}
