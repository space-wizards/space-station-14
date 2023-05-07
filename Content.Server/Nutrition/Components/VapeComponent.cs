using System.Threading;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Atmos;

/// <summary>
/// Component for vapes
/// </summary>
namespace Content.Server.Nutrition.Components
{
    [RegisterComponent, Access(typeof(SmokingSystem))] 
    public sealed class VapeComponent : Component
    {
        [DataField("delay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Delay { get; set; } = 5;

        [DataField("userDelay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float UserDelay { get; set; } = 2;

        [DataField("explosionIntensity")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ExplosionIntensity { get; set; } = 2.5f;

        [DataField("explodeOnUse")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ExplodeOnUse { get; set; } = false;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField("gasType")]
        [ViewVariables(VVAccess.ReadWrite)]
        public Gas GasType { get; set; } = Gas.WaterVapor;

        /// <summary>
        /// Solution volume will be divided by this number and converted to the gas
        /// </summary>
        [DataField("reductionFactor")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ReductionFactor { get; set; } = 300f;

        [DataField("solutionNeeded")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string SolutionNeeded = "Water";

        public CancellationTokenSource? CancelToken;
    }
}