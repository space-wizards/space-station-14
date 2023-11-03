// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.EventCapturePoint;

/// <summary>
/// Component that should be at capturable point entities.
/// </summary>
[RegisterComponent]
public sealed partial class EventCapturePointComponent : Component
{
    [ViewVariables, DataField, NonSerialized]
    public EntityUid? FlagEntity;

    [ViewVariables, DataField]
    public TimeSpan FlagManipulationDuration = TimeSpan.FromSeconds(15);

    [ViewVariables, DataField]
    public float FlagRemovalImpulse = 35;
}

[Serializable, NetSerializable]
public sealed partial class FlagRemovalFinshedEvent : DoAfterEvent
{
    public override FlagRemovalFinshedEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class FlagInstallationFinshedEvent : DoAfterEvent
{
    public override FlagInstallationFinshedEvent Clone() => this;
}

[Serializable, NetSerializable]
public enum CapturePointVisuals
{
    Visuals,
}
