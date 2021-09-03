using Robust.Shared.Analyzers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.PneumaticCannon
{
    [RegisterComponent, Friend(typeof(PneumaticCannonSystem))]
    public class PneumaticCannonComponent : Component
    {
        public override string Name { get; } = "PneumaticCannon";

        [ViewVariables]
        public ContainerSlot GasTankSlot = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        public PneumaticCannonPower Power = PneumaticCannonPower.Low;
    }

    /// <summary>
    ///     How strong the pneumatic cannon should be.
    ///     Each tier throws items farther and with more speed, but has drawbacks.
    ///     The highest power knocks the player down for a considerable amount of time.
    /// </summary>
    public enum PneumaticCannonPower
    {
        Low,
        Medium,
        High
    }
}
