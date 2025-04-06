using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Kodepiia.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class KodepiiaConsumeActionComponent : Component
{
    [DataField]
    public EntityUid? ConsumeAction;

    [DataField]
    public string? ConsumeActionId = "ActionKodepiiaConsume";

    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public bool CanGib = true;
}
