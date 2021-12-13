using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Light.Component
{
    /// <summary>
    /// Networked solely for admemes.
    /// </summary>
    [NetworkedComponent]
    [RegisterComponent]
    public class RgbLightControllerComponent : Robust.Shared.GameObjects.Component
    {
        public override string Name => "RgbLightController";

        [DataField("cycleRate")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float CycleRate { get; set; } = 10.0f;
    }
}
