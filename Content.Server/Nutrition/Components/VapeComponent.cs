using System.Threading;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Damage;

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

        [DataField("smokeAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public int SmokeAmount { get; set; } = 0;

        [DataField("smokeDuration")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float SmokeDuration { get; set; } = 3f;

        [DataField("explosionIntensity")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ExplosionIntensity { get; set; } = 2.5f;

        [DataField("explodeOnUse")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ExplodeOnUse { get; set; } = false;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField("smokePrototype")]
        public string SmokePrototype = "VapeSmoke";

        [DataField("explodableSolutions")]
        public List<string> ExplodableSolutions { get; set; } = new(){
            "Water",
            "Plasma",
            "WeldingFuel"
        };

        public CancellationTokenSource? CancelToken;
    }
}