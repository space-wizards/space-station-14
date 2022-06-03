using Content.Shared.Damage;
using Robust.Shared.Physics;

namespace Content.Shared.Blocking;

[RegisterComponent]
public class SharedBlockingUserComponent : Component
{
    /// <summary>
    /// The entity that's being used to block
    /// </summary>
    public EntityUid? BlockingItem;

    public DamageModifierSet Modifiers = default!;

    /// <summary>
    /// Stores the entities original bodytype
    /// Used so that it can be put back to what it was after anchoring and to avoid messy temp calcs
    /// </summary>
    [ViewVariables]
    public BodyType OriginalBodyType;

}
