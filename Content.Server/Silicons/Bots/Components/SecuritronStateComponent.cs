using System;
using System.Numerics;
using Robust.Shared.GameObjects;

namespace Content.Server.Silicons.Bots.Components;

[RegisterComponent]
public sealed partial class SecuritronStateComponent : Component
{
    public EntityUid? CurrentTarget;
    public SecuritronTargetTrackingState TargetStatus = SecuritronTargetTrackingState.None;
    public bool TargetFleeing;
    public bool ReportedSpotted;
    public bool ReportedFleeing;
    public bool ReportedDowned;
    public bool ReportedCuffed;
    public bool CuffInProgress;
    public float ClosestDistance = float.MaxValue;
    public Vector2? LastKnownTargetPosition;
    public TimeSpan NextCuffAttempt;
    public TimeSpan NextSpeechTime;
}

public enum SecuritronTargetTrackingState
{
    None,
    Announced,
    Standby,
    Engaging,
    Downed,
    Cuffed,
}
