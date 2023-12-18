using Robust.Shared.GameStates;

namespace Content.Shared.Flash.Components
{
    /// <summary>
    /// Upon being triggered will flash in an area around it.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class FlashOnTriggerComponent : Component
    {
        [DataField("range")] public float Range = 1.0f;
        [DataField("duration")] public float Duration = 8.0f;
    }
}
