using Content.Shared.Damage;
using Robust.Shared.Physics;

namespace Content.Shared.Blocking;

/// <summary>
/// This component gets dynamically added to an Entity via the <see cref="BlockingSystem"/>
/// </summary>
[RegisterComponent]
public sealed class BlockingUserComponent : Component
{
    /// <summary>
    /// The entity that's being used to block
    /// </summary>
    [ViewVariables]
    [DataField("blockingItem")]
    public EntityUid? BlockingItem;

    [ViewVariables]
    [DataField("modifiers")]
    public DamageModifierSet Modifiers = default!;

    /// <summary>
    /// Stores the entities original bodytype
    /// Used so that it can be put back to what it was after anchoring
    /// </summary>
    [ViewVariables]
    [DataField("originalBodyType")]
    public BodyType OriginalBodyType;

}
