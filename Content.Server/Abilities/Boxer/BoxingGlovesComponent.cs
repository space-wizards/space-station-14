
namespace Content.Server.Abilities.Boxer
{
    [RegisterComponent]
    public sealed class BoxingGlovesComponent : Component
    {
        [DataField("rangeModifier")]
        public float RangeModifier = 1.5f;

        [DataField("stamDamageModifier")]
        public float StamDamageModifier = 1.75f;
    }
}
