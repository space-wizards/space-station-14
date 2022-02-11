using Content.Server.Atmos;
using Content.Server.Body.Systems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Friend(typeof(BloodstreamSystem))]
    public class BloodstreamComponent : Component
    {
        /// <summary>
        ///     Max volume of internal solution storage
        /// </summary>
        [DataField("maxVolume")]
        public FixedPoint2 InitialMaxVolume = FixedPoint2.New(250);

        /// <summary>
        ///     Internal solution for reagent storage
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Solution Solution = default!;
    }
}
