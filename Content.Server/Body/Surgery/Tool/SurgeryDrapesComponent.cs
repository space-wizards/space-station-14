using Content.Server.UserInterface;
using Content.Shared.Body.Surgery.UI;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Surgery.Tool
{
    [RegisterComponent]
    public class SurgeryDrapesComponent : Component
    {
        public override string Name => "SurgeryDrapes";

        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(SurgeryUIKey.Key);
    }
}
