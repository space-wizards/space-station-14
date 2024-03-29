using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent, AutoGenerateComponentPause]
    [Access(typeof(GasRegenerationSystem))]
    public sealed partial class GasRegenerationComponent : Component
    {
        /// <summary>
        /// The name of the mixture to add to
        /// </summary>
        [DataField("mixture", required: true), ViewVariables(VVAccess.ReadWrite)]
        public string GasName = string.Empty;

        /// <summary>
        /// The tank to add to
        /// </summary>
        [DataField("mixtureRef")]
        public Entity<GasTankComponent>? Mixture = null;

        /// <summary>
        /// The gas to be regenerated in the solution.
        /// </summary>
        [DataField("generated", required: true), ViewVariables(VVAccess.ReadWrite)]
        public GasMixture Generated = default!;

        /// <summary>
        /// How long it takes to regenerate once.
        /// </summary>
        [DataField("duration"), ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan Duration = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The time when the next regeneration will occur.
        /// </summary>
        [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
        [AutoPausedField]
        public TimeSpan NextRegenTime = TimeSpan.FromSeconds(0);
    }
}
