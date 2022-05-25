using Content.Shared.Pointing.Components;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Pointing.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedPointingArrowComponent))]
    public sealed class PointingArrowComponent : SharedPointingArrowComponent
    {
    }
}
