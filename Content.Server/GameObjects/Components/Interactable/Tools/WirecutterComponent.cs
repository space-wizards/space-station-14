using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Interactable.Tools
{
    /// <summary>
    /// Tool that can be used for some cutting interactions such as wires or hacking
    /// </summary>
    [RegisterComponent]
    public class WirecutterComponent : ToolComponent
    {
        public override string Name => "Wirecutter";
    }
}
