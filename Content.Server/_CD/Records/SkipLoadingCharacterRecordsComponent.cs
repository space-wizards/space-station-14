using Robust.Shared.GameObjects;

namespace Content.Server._CD.Records;

/// <summary>
/// Marker component for entities that should not have profile records loaded.
/// </summary>
[RegisterComponent]
public sealed partial class SkipLoadingCharacterRecordsComponent : Component;
