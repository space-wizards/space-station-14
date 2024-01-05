using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Timing;
using JetBrains.Annotations;

namespace Content.Shared.Interaction.Events;

/// <summary>
///     Raised when using the entity in your hands.
/// </summary>
[PublicAPI]
public sealed class UseInHandEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     Entity holding the item in their hand.
    /// </summary>
    public EntityUid User;

    /// <summary>
    ///     Whether or not to apply a UseDelay when used.
    ///     Mostly used by the <see cref="ClothingSystem"/> quick-equip to not apply the delay to entities that have the <see cref="UseDelayComponent"/>.
    /// </summary>
    public bool ApplyDelay = true;

    public UseInHandEvent(EntityUid user)
    {
        User = user;
    }
}
