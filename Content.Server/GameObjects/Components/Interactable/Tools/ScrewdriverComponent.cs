using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Interactable.Tools
{
    [RegisterComponent]
    public class ScrewdriverComponent : ToolComponent
    {
        /// <summary>
        /// Tool that interacts with technical components that need to be screwed in
        /// </summary>
        public override string Name => "Screwdriver";
    }
}
