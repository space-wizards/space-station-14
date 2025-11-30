using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Eye.Blinking;
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class EyeBlinkingComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public TimeSpan BlinkDuration = TimeSpan.FromSeconds(0.5f);
    [DataField]
    [AutoNetworkedField]
    public TimeSpan BlinkInterval = TimeSpan.FromSeconds(5);
    [DataField]
    [AutoNetworkedField]
    public float BlinkSkinColorMultiplier = 0.9f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextOpenEyesTime;
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextBlinkingTime;
    [DataField]
    [AutoNetworkedField]
    public bool IsSleeping;
    [DataField]
    [AutoNetworkedField]
    public bool IsBlinking;

}
