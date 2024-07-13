using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Timing;
using Content.Shared.Damage;
using Content.Shared.Physics;
using Robust.Shared.Physics;

namespace Content.Shared.MartialArts.Components;    // Added a new category because im planning to make martials combat soon. And throwing is kinda... Martial-artistic?

[RegisterComponent, NetworkedComponent]
public sealed partial class GrabThrownComponent : Component
{
    public DamageSpecifier? DamageOnCollide;

    public DamageSpecifier? WallDamageOnCollide;

    public float? StaminaDamageOnCollide;

    public int? SavedCollisionMask;

    public int? SavedCollisionLayer;
}
