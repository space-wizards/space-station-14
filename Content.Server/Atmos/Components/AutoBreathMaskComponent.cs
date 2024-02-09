namespace Content.Server.Atmos.Components;

/// <summary>
/// When the entity with this component is equipped it will automatically (try to) activate the wearer's Internals
/// It requires BreathMaskComponent on the entity
/// </summary>
[RegisterComponent]
public sealed partial class AutoBreathMaskComponent : Component
{
    /// <summary>
    /// If true, this component will be removed after the first activation attempt
    /// For breathing masks that need automatic activation only once at roundstart
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool SingleUse = true;
}

