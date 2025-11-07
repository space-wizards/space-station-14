using Content.Shared.DeadSpace.NightVision;

namespace Content.Client.DeadSpace.Components.NightVision;

[RegisterComponent]
public sealed partial class NightVisionComponent : SharedNightVisionComponent
{
    /// <description>
    ///     Время до обновления состояния
    /// </description>
    [ViewVariables(VVAccess.ReadOnly)]
    public uint ClientLastToggleTick;

    /// <description>
    ///     Время активации
    /// </description>
    [ViewVariables(VVAccess.ReadOnly)]
    public uint ServerLastToggleTick;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsToggled = false;

    /// <description>
    ///     Скорость анимации
    /// </description>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float TransitionSpeed = 1f;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? SoundEntity = null;
}
