using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._MACRO.Tools.Components;

/// <summary>
/// Used on an entity with <see cref="ToolComponent"/> to override its SpeedModifier
/// if the user has  <see cref="CowToolProficiencyComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedToolSystem))]
public sealed partial class CowToolComponent : Component
{
    /// <summary>
    /// Value used to override the SpeedModifier in <see cref="ToolComponent"/>.
    /// </summary>
    [DataField]
    public float ProficiencySpeedModifier = 1;
}
