using Content.Shared.Eye.Blinking;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Eye.Blinking;

[RegisterComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class EyeBlinkingClientComponent : Component
{

    [AutoPausedField]
    public TimeSpan PausedOffset;

    [ViewVariables]
    public List<EyelidState> Eyelids = new();
}
public sealed partial class EyelidState
{
    public ISpriteLayer Layer;
    [ViewVariables] public bool IsClosed;
    [ViewVariables] public bool IsCompleteBlink;
    [ViewVariables] public TimeSpan ScheduledCloseTime;
    [ViewVariables] public TimeSpan ScheduledOpenTime;

    public EyelidState(ISpriteLayer layer)
    {
        Layer = layer;
        IsClosed = false;
        IsCompleteBlink = false;
        ScheduledCloseTime = default;
        ScheduledOpenTime = default;
    }
}
