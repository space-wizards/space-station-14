using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Interactable.Tools
{
    /// <summary>
    /// Wrenches bolts, and interacts with things that have been bolted
    /// </summary>
    [RegisterComponent]
    public class WrenchComponent : ToolComponent
    {
        public override string Name => "Wrench";
    }
}
