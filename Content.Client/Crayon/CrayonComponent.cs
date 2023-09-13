using Content.Shared.Crayon;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Client.Crayon
{
    [RegisterComponent]
    public sealed partial class CrayonComponent : SharedCrayonComponent
    {
        [ViewVariables(VVAccess.ReadWrite)] public bool UIUpdateNeeded;
        [ViewVariables] public int Charges { get; set; }
        [ViewVariables] public int Capacity { get; set; }
    }
}
