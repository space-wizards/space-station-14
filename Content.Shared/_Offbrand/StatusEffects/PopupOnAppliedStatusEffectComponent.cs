using Content.Shared.Popups;

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(PopupOnAppliedStatusEffectSystem))]
public sealed partial class PopupOnAppliedStatusEffectComponent : Component
{
    [DataField(required: true)]
    public LocId Message;

    [DataField]
    public PopupType VisualType = PopupType.Small;
}
