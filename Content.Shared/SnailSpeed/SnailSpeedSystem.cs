using Robust.Shared.Serialization;
using Robust.Shared.Network;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.SnailSpeed;

/// <summary>
/// Allows mobs to produce materials using Thirst with <see cref="ExcretionComponent"/>.
/// </summary>
public abstract partial class SharedSnailSpeedSystem : EntitySystem
{
    // Managers
    [Dependency] private readonly INetManager _netManager = default!;

    // Systems
	[Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
	[Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
	[Dependency] private readonly SharedJetpackSystem _jetpack = default!;

    public override void Initialize()
    {
        base.Initialize();
		SubscribeLocalEvent<SnailSpeedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
    }

	private void OnRefreshMovespeed(EntityUid uid, SnailSpeedComponent component, RefreshMovementSpeedModifiersEvent args)
	{
	    if (_jetpack.IsUserFlying(uid))
        return;
	
		
		args.ModifySpeed(component.SnailSlowdownModifier, component.SnailSlowdownModifier);
	}
}