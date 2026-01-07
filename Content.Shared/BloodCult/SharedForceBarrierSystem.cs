// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared.BloodCult;

/// <summary>
/// Allows blood cult members and entities being dragged by cultists to pass through force barriers.
/// </summary>
public abstract class SharedForceBarrierSystem : EntitySystem
{
	public override void Initialize()
	{
		base.Initialize();

		SubscribeLocalEvent<ForceBarrierComponent, PreventCollideEvent>(OnPreventCollide);
	}

	private void OnPreventCollide(Entity<ForceBarrierComponent> barrier, ref PreventCollideEvent args)
	{
		if (args.Cancelled)
			return;

		var otherEntity = args.OtherEntity;

		// Allow blood cult members to pass through
		if (IsBloodCultMember(otherEntity))
		{
			args.Cancelled = true;
			return;
		}

		// Allow entities being dragged by cultists to pass through
		if (IsBeingDraggedByCultist(otherEntity))
		{
			args.Cancelled = true;
			return;
		}
	}

	/// <summary>
	/// Checks if an entity is a blood cult member (cultist, juggernaut, or shade).
	/// </summary>
	private bool IsBloodCultMember(EntityUid entity)
	{
		return HasComp<BloodCultistComponent>(entity) ||
		       HasComp<JuggernautComponent>(entity) ||
		       HasComp<ShadeComponent>(entity);
	}

	/// <summary>
	/// Checks if an entity is being dragged by a cultist.
	/// </summary>
	private bool IsBeingDraggedByCultist(EntityUid entity)
	{
		if (!TryComp<PullableComponent>(entity, out var pullable))
			return false;

		if (pullable.Puller == null)
			return false;

		// Check if the puller is a cultist
		return HasComp<BloodCultistComponent>(pullable.Puller.Value);
	}
}

