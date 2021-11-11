using Content.Server.Clothing.Components;
using Content.Server.Inventory.Components;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;

namespace Content.Server.Medical.Components
{
    /// <summary>
    /// Tag clothing component that denotes an entity as Sterile in the medical context
    /// </summary>
    [RegisterComponent]

    public class SterileComponent : Component
    {
        public override string Name => "Sterile";
    }
}
