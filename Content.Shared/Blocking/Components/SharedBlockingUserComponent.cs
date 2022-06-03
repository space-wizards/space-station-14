using Content.Shared.Damage;

namespace Content.Shared.Blocking;

[RegisterComponent]
public class SharedBlockingUserComponent : Component
{
    /// <summary>
    /// The entity that's being used to block
    /// </summary>
    public EntityUid? BlockingItem;

    public DamageModifierSet Modifiers = default!;
}
