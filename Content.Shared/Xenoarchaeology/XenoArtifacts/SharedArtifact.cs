using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.XenoArtifacts;

[Serializable, NetSerializable]
public enum SharedArtifactsVisuals : byte
{
    SpriteIndex,
    IsActivated
}


public sealed class ArtifactSelfActivateEvent : InstantActionEvent
{
}
