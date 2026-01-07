// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Server.GameObjects;
using Content.Shared.Interaction;
using Content.Shared.Actions;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;
using Content.Server.Popups;
using Content.Shared.Popups;

namespace Content.Server.BloodCult.EntitySystems;

public sealed class EmpowerOnStandSystem : EntitySystem
{
	[Dependency] private readonly SharedTransformSystem _transform = default!;
	[Dependency] private readonly MapSystem _mapSystem = default!;
	[Dependency] private readonly IMapManager _mapManager = default!;
	[Dependency] private readonly SharedActionsSystem _action = default!;
	[Dependency] private readonly PopupSystem _popup = default!;

	public override void Initialize()
	{
		base.Initialize();

		SubscribeLocalEvent<EmpowerOnStandComponent, InteractHandEvent>(OnInteractHand);
	}

	private void OnInteractHand(Entity<EmpowerOnStandComponent> rune, ref InteractHandEvent args)
	{
		if (args.Handled)
			return;

		// Only cultists can interact with empowering runes
		if (!HasComp<BloodCultistComponent>(args.User))
			return;

		// Check if the user is standing on this specific rune
		if (!IsStandingOnRune(args.User, rune))
		{
			_popup.PopupEntity(
				Loc.GetString("cult-empowering-rune-not-in-range"),
				args.User, args.User, PopupType.MediumCaution
			);
			args.Handled = true;
			return;
		}

		// Find the spell selection action for this user
		var actions = _action.GetActions(args.User);
		foreach (var actionId in actions)
		{
			if (!TryComp<CultistSpellComponent>(actionId, out var spellComp))
				continue;

			if (spellComp.AbilityId != "SpellsSelect")
				continue;

			// Raise the spell selection event
			var spellEvent = new BloodCultSpellsEvent
			{
				Action = actionId,
				Performer = args.User
			};
			RaiseLocalEvent(spellEvent);
			args.Handled = true;
			return;
		}

		// If no spell selection action found, show error
		_popup.PopupEntity(
			Loc.GetString("cult-empowering-rune-no-action"),
			args.User, args.User, PopupType.MediumCaution
		);
		args.Handled = true;
	}

	/// <summary>
	/// Checks if a user is standing on a specific empowering rune.
	/// </summary>
	private bool IsStandingOnRune(EntityUid user, Entity<EmpowerOnStandComponent> rune)
	{
		var userCoords = Transform(user).Coordinates;
		var userLocation = userCoords.AlignWithClosestGridTile(entityManager: EntityManager, mapManager: _mapManager);
		var userGridUid = _transform.GetGrid(userLocation);
		if (!TryComp<MapGridComponent>(userGridUid, out var userGrid))
			return false;

		var userTile = _mapSystem.GetTileRef(userGridUid.Value, userGrid, userLocation);

		// Check if the rune is on the same tile
		var runeCoords = Transform(rune).Coordinates;
		var runeLocation = runeCoords.AlignWithClosestGridTile(entityManager: EntityManager, mapManager: _mapManager);
		var runeGridUid = _transform.GetGrid(runeLocation);
		if (!TryComp<MapGridComponent>(runeGridUid, out var runeGrid))
			return false;

		var runeTile = _mapSystem.GetTileRef(runeGridUid.Value, runeGrid, runeLocation);

		// Check if user and rune are on the same grid and tile
		if (userGridUid != runeGridUid)
			return false;

		if (userTile.GridIndices != runeTile.GridIndices)
			return false;

		// Verify the rune is actually anchored on this tile
		foreach (var anchoredEnt in _mapSystem.GetAnchoredEntities(userGridUid.Value, userGrid, userTile.GridIndices))
		{
			if (anchoredEnt == rune.Owner)
				return true;
		}

		return false;
	}
}

