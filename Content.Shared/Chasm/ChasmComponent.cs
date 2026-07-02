using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Chasm;

/// <summary>
/// Marks a component that will cause entities to fall into them on a step trigger activation
/// </summary>
[NetworkedComponent, RegisterComponent, Access(typeof(ChasmSystem))]
public sealed partial class ChasmComponent : Component
{
    /// <summary>
    /// Entities allowed to fall into the hole. If null, anything not on the blacklist can fall into the hole. If both
    /// are null, anything can.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Entities not allowed to fall into the hole. If null, anything on the whitelist can fall into the hole. If both
    /// are null, anything can.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Sound that should be played when an entity falls into the chasm
    /// </summary>
    [DataField]
    public SoundSpecifier FallingSound = new SoundPathSpecifier("/Audio/Effects/falling.ogg");
}

/// <summary>
/// This event is raised on a chasm when <paramref name="Faller"/> tries to start falling into it. This is used to allow
/// systems an opportunity to cancel the falling for whatever reason.
/// </summary>
[ByRefEvent]
public record struct EntityStartFallingAttemptEvent(EntityUid Faller)
{
    public readonly EntityUid Faller = Faller;
    public bool Cancelled = false;
}

/// <summary>
/// Raised on a chasm when it would cause an entity to fall but the chasm's white-/blacklist prevented it.
/// </summary>
[ByRefEvent]
public readonly record struct FallerRejectedByChasmEvent(EntityUid Entity);

/// <summary>
/// This event is raised on an entity when it begins falling into <paramref name="FallingInto"/>.
/// </summary>
[ByRefEvent]
public readonly record struct StartedFallingIntoChasmEvent(Entity<ChasmComponent> FallingInto);

/// <summary>
/// This event is raised on a chasm when <paramref name="Faller"/> begins falling into it.
/// </summary>
[ByRefEvent]
public readonly record struct EntityStartedFallingIntoChasmEvent(Entity<ChasmFallingComponent> Faller);

/// <summary>
/// This event is raised on an entity when it has finished falling into <paramref name="FellInto"/>, just before it is deleted.
/// </summary>
[ByRefEvent]
public readonly record struct CompletedFallingIntoChasmEvent(Entity<ChasmComponent> FellInto);

/// <summary>
/// This event is raised on a chasm when <paramref name="Faller"/> has finished falling into it, just before it is deleted.
/// </summary>
[ByRefEvent]
public readonly record struct EntityCompletedFallingIntoChasmEvent(Entity<ChasmFallingComponent> Faller);
