// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Content.Shared.BloodCult.Components;
using Content.Shared.Movement.Events;

namespace Content.Server.BloodCult.EntitySystems;

/// <summary>
/// Handles inactive juggernauts - prevents movement when soulstone is ejected
/// </summary>
public sealed class InactiveJuggernautSystem : EntitySystem
{

	public override void Initialize()
	{
		base.Initialize();
		
		SubscribeLocalEvent<JuggernautComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
	}

	private void OnUpdateCanMove(Entity<JuggernautComponent> juggernaut, ref UpdateCanMoveEvent args)
	{
		// Prevent movement if inactive (no soulstone)
		if (juggernaut.Comp.IsInactive)
		{
			args.Cancel();
		}
	}
}

