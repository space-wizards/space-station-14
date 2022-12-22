using Content.Server.Nutrition.EntitySystems;
using System.Threading;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent, Access(typeof(VapeSystem))] 
    public sealed class VapeComponent : Component
    {
        [DataField("delay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Delay { get; set; } = 5;

        [DataField("userdelay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float UserDelay { get; set; } = 2;

        [DataField("smokeamount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float SmokeAmount { get; set; } = 1;

        [DataField("explosionintensity")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ExplosionIntensity { get; set; } = 2.5f;

        [DataField("explodeonuse")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ExplodeOnUse { get; set; } = false;


        public CancellationTokenSource? CancelToken;
    }
}