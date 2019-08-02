using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Interactable.Tools
{
    /// <summary>
    /// Tool used for interfacing/hacking into configurable computers
    /// </summary>
    [RegisterComponent]
    public class MultitoolComponent : ToolComponent
    {
        public override string Name => "Multitool";
    }
}
