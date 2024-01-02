using Robust.Shared.GameStates;
namespace Content.Shared.Flash
{
    /// <summary>
    /// Upon being triggered will flash in an area around it.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState]
    public sealed partial class FlashOnTriggerComponent : Component
    {
        [AutoNetworkedField]
        [DataField("range")] public float Range = 1.0f;
    
        [AutoNetworkedField]
        [DataField("duration")] public float Duration = 8.0f;
    }
}
