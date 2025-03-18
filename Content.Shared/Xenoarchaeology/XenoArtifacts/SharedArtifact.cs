using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.XenoArtifacts;

[Serializable, NetSerializable]
public enum SharedArtifactsVisuals : byte
{
    SpriteIndex,
    IsActivated
}

[Serializable, NetSerializable]
public enum ArtiOrigin : byte
{
    Undecided,
    Eldritch,
    Martian,
    Precursor,
    Silicon,
    Wizard
}

/// <summary>
///     Raised as an instant action event when a sentient artifact activates itself using an action.
/// </summary>
public sealed partial class ArtifactSelfActivateEvent : InstantActionEvent
{
}
