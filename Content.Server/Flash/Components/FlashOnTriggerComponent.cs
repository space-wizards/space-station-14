namespace Content.Server.Flash.Components
{
    /// <summary>
    /// Upon being triggered will flash in an area around it.
    /// </summary>
    [RegisterComponent]
    internal sealed partial class FlashOnTriggerComponent : Component
    {
        [DataField("range")] internal float Range = 1.0f;
        [DataField("duration")] internal float Duration = 8.0f;
    }
}
