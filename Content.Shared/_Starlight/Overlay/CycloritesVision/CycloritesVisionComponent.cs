using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding.Components;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class CycloritesVisionComponent : Component
{
    [DataField]
    public bool blockedByFlashImmunity = false;
}
