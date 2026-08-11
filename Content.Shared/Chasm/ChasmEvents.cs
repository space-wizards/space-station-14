using Content.Shared.Chasm.Components;

namespace Content.Shared.Chasm;

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
/// Raised on an entity with <see cref="ChasmFallingComponent"/> to reset its visuals.
/// </summary>
[ByRefEvent]
public readonly record struct ResetChasmVisualsEvent;

/// <summary>
/// Raised on an entity that already fell into a chasm in order to
/// prevent the effects of the chasm in the last moment.
/// </summary>
[ByRefEvent]
public record struct BeforeChasmFallEvent(EntityUid? Chasm, bool Cancelled = false);

/// <summary>
/// This event is raised on an entity when it has finished falling into <paramref name="FellInto"/>, just before the effects are applied.
/// </summary>
[ByRefEvent]
public readonly record struct CompletedFallingIntoChasmEvent(Entity<ChasmComponent> FellInto);

/// <summary>
/// This event is raised on a chasm when <paramref name="Faller"/> has finished falling into it, just before the effects are applied.
/// </summary>
[ByRefEvent]
public readonly record struct EntityCompletedFallingIntoChasmEvent(Entity<ChasmFallingComponent> Faller);

/// <summary>
/// An event raised on a chasm that does something with the entity that fell into it.
/// </summary>
/// <remarks>
/// If this event is not <see cref="Handled"/>, it will throw a debug assert.
/// </remarks>
/// <param name="Faller">The entity that fell into the chasm.</param>
[ByRefEvent]
public record struct ChasmFallEffectsEvent(EntityUid Faller)
{
    public readonly EntityUid Faller = Faller;
    public bool Handled = false;
}
