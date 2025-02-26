using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Fishing;

[RegisterComponent, NetworkedComponent]
//Imp : Basically a copy of GrapplingProjectileComponent
public sealed partial class FishingProjectileComponent : Component
{
    [DataField] public float JointLength = 2.5f; // qwat verified (approved by dinner)
}
