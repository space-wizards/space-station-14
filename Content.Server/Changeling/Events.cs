using Content.Shared.Actions;

namespace Content.Server.Changeling;

/// <summary>
/// Event raised on the changeling to attempt to use a sting.
/// </summary>
public abstract class StingEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The mob that was alt clicked.
    /// </summary>
    public EntityUid Target;
}

/// <summary>
/// Extracts dna from a mob.
/// The mob must have AbsorbableComponent for it to work.
/// </summary>
public sealed class ExtractionStingEvent : StingEvent { }

/// <summary>
/// Sets ChangelingComponent.ActiveSting to the sting id after validation.
/// </summary>
public sealed class SelectStingEvent : InstantActionEvent
{
    public readonly string Sting;

    public SelectStingEvent(string sting)
    {
        Sting = sting;
    }
}

/// <summary>
/// Show the transformations window on the client.
/// </summary>
public sealed class ChangelingTransformEvent : InstantActionEvent { }
