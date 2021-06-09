using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using System;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class MeleeChemicalInjectorComponent : Component
    {
        public override string Name => "MeleeChemicalInjector";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("transferAmount")]
        public ReagentUnit TransferAmount { get; set; } = ReagentUnit.New(1);

        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferEfficiency { get => _transferEfficiency; set => _transferEfficiency = Math.Clamp(value, 0, 1); }

        [DataField("transferEfficiency")]
        private float _transferEfficiency = 1f;
    }
}
