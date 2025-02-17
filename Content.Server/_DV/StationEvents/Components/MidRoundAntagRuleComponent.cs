namespace Content.Server.StationEvents.Components;

// Impstation this is MIT code licenced under Nyanotrasen namespace

/// <summary>
/// Spawns any antags at random midround antag spawnpoints, falls back to vent critter spawners.
/// Requires <c>AntagSelection</c>.
/// </summary>
[RegisterComponent]
public sealed partial class MidRoundAntagRuleComponent : Component;
