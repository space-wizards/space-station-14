namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(ExaminableStatusEffectSystem))]
public sealed partial class ExaminableStatusEffectComponent : Component
{
    /// <summary>
    /// The desired message to show on examine. The target of this effect will be passed as $target to the message.
    /// </summary>
    [DataField(required: true)]
    public LocId Message;
}
