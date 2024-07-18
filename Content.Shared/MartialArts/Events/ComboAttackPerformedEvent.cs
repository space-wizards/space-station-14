using Content.Shared.Movement.Components;

namespace Content.Shared.MartialArts;

/// <summary>
///     Raised whenever <see cref="IMoverComponent.CanMove"/> needs to be updated. Cancel this event to prevent a
///     mover from moving.
/// </summary>
public sealed class ComboAttackPerformedEvent : CancellableEntityEventArgs
{
    public ComboAttackPerformedEvent(EntityUid performer, EntityUid target, EntityUid weapon, ComboAttackType type)
    {
        Performer = performer;
        Target = target;
        Weapon = weapon;
        Type = type;
    }

    public EntityUid Performer { get; }
    public EntityUid Target { get; }
    public EntityUid Weapon { get; }
    public ComboAttackType Type { get; }
}

public enum ComboAttackType : byte
{
    Harm,
    HarmLight,
    Disarm,
    Grab
}
