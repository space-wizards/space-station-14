namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(KnockdownOnAppliedStatusEffectSystem))]
public sealed partial class KnockdownOnAppliedStatusEffectComponent : Component
{
    [DataField(required: true)]
    public TimeSpan Duration;
}
