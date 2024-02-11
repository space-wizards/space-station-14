using Content.Server.Pointing.EntitySystems;
using Content.Shared.Pointing.Components;

namespace Content.Server.Pointing.Components
{
    [RegisterComponent]
    [Access(typeof(PointingSystem))]
    public sealed partial class PointingArrowComponent : SharedPointingArrowComponent
    {
        /// <summary>
        ///     Whether or not this arrow will convert into a
        ///     <see cref="RoguePointingArrowComponent"/> when its duration runs out.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rogue")]
        public bool Rogue;
    }
}
