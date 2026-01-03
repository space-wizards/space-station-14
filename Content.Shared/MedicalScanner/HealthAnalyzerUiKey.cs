using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

[Serializable, NetSerializable]
public enum HealthAnalyzerUiKey : byte
{
    Key
}

/// <summary>
/// UI state for health analyzer UIs.
/// </summary>
/// <seealso cref="HealthAnalyzerUiKey"/>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerBUIState : BoundUserInterfaceState
{
    public float Temperature;
}
