using Content.Shared.Sound.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Sound.Components;

/// <summary>
/// Whenever this item is used upon by a specific entity prototype in the hand of a user, play a sound
/// </summary>
[RegisterComponent]
public sealed partial class EmitSoundOnInteractUsingComponent : BaseEmitSoundComponent
{
    [DataField("UsedItemProto", false, 1, true)]
    public ProtoId<EntityPrototype> UsedItemProto = new();
}
