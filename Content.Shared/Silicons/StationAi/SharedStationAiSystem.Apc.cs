using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    public void AddApcToggleMainBreakerAction(ref GetStationAiRadialEvent args)
    {
        args.Actions.Add(new StationAiRadial()
            {
                Sprite = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
                Tooltip = Loc.GetString("apc-component-verb-text-alternative"),
                Event = new StationAiApcToggleMainBreakerEvent()
            }
        );
    }
}

/// <summary> Event for StationAI attempt at toggling an APC's main breaker. </summary>
[Serializable, NetSerializable]
public sealed class StationAiApcToggleMainBreakerEvent : BaseStationAiAction
{
}
