using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Chasm;

/// <summary>
///     Marks a component that will cause entities to fall into them on a step trigger activation
/// </summary>
[NetworkedComponent, RegisterComponent, Access(typeof(ChasmSystem))]
public sealed partial class ChasmComponent : Component
{
    /// <summary>
    ///     Sound that should be played when an entity falls into the chasm
    /// </summary>
    [DataField]
    public SoundSpecifier FallingSound = new SoundPathSpecifier("/Audio/Effects/falling.ogg");
}

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
