using System.Collections.Generic;
using Content.Server.Items;
using Content.Server.MachineLinking.Components;
using Content.Server.Power.Components;
using Content.Shared.Conveyor;
using Content.Shared.MachineLinking;
using Robust.Server.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Conveyor
{
    [RegisterComponent]
    [Friend(typeof(ConveyorSystem))]
    public class ConveyorComponent : Component
    {
        public override string Name => "Conveyor";

        /// <summary>
        ///     The angle to move entities by in relation to the owner's rotation.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("angle")]
        public Angle Angle = Angle.Zero;

        /// <summary>
        ///     The amount of units to move the entity by per second.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("speed")]
        public float Speed = 2f;

        /// <summary>
        ///     The current state of this conveyor
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public ConveyorState State;
    }
}
