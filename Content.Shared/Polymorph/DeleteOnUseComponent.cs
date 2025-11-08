using Robust.Shared.GameObjects;

namespace Content.Shared.Polymorph
{
    /// <summary>
    /// Marker component: when an item with this component receives a PolymorphActionEvent,
    /// the system will polymorph the Performer and delete the item (single-use).
    /// </summary>
    [RegisterComponent]
    public sealed partial class DeleteOnUseComponent : Component
    {
    }
}
