using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.CosmicCult.Components;

/// <summary>
/// Component for targets being cleansed of corruption.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class CleanseCultComponent : Component
{
    [ViewVariables]
    [AutoPausedField]
    public TimeSpan CleanseTime = default!;
    [DataField] public TimeSpan CleanseDuration = TimeSpan.FromSeconds(25);
}
