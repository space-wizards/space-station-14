// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Robust.Shared.GameObjects;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;

namespace Content.Server.BloodCult;

public sealed class BloodCultistSystem : SharedBloodCultistSystem
{
	[Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

	public override void Initialize()
	{
		base.Initialize();
		SubscribeLocalEvent<BloodCultCommuneEvent>(OpenCommuneUI);
		SubscribeLocalEvent<BloodCultSpellsEvent>(OpenSpellsUI);
	}

	#region CommuneUI
	private void OpenCommuneUI(BloodCultCommuneEvent ev)
	{
		// Use ev.Performer (the player entity) instead of Container (the mind entity)
		// since BloodCultistComponent is on the player entity, not the mind

		// Allow both blood cultists and juggernauts to use commune
		// Check both separately to ensure variables are assigned
		bool isCultist = TryComp<BloodCultistComponent>(ev.Performer, out var cultistComp);
		bool isJuggernaut = TryComp<JuggernautComponent>(ev.Performer, out var juggernautComp);
		
		if (!isCultist && !isJuggernaut)
			return;

		if (!_uiSystem.HasUi(ev.Performer, BloodCultistCommuneUIKey.Key))
			return;

		if (_uiSystem.IsUiOpen(ev.Performer, BloodCultistCommuneUIKey.Key))
			return;

		if (_uiSystem.TryOpenUi(ev.Performer, BloodCultistCommuneUIKey.Key, ev.Performer))
		{
			if (isCultist && cultistComp != null)
				UpdateCommuneUI((ev.Performer, cultistComp));
			else if (isJuggernaut && juggernautComp != null)
				// Juggernauts use the same UI but with empty state (no stored message)
				_uiSystem.SetUiState(ev.Performer, BloodCultistCommuneUIKey.Key, new BloodCultCommuneBuiState(""));
		}

		ev.Handled = true;
	}

	private void UpdateCommuneUI(Entity<BloodCultistComponent> entity)
	{
		if (_uiSystem.HasUi(entity, BloodCultistCommuneUIKey.Key))
			_uiSystem.SetUiState(entity.Owner, BloodCultistCommuneUIKey.Key, new BloodCultCommuneBuiState(""));
	}
	#endregion

	#region SpellsUI
	private void OpenSpellsUI(BloodCultSpellsEvent ev)
	{
		// Use ev.Performer (the player entity) instead of Container (the mind entity)
		// since BloodCultistComponent is on the player entity, not the mind
		if (!TryComp<BloodCultistComponent>(ev.Performer, out var cultistComp))
			return;

		if (!_uiSystem.HasUi(ev.Performer, SpellsUiKey.Key))
			return;

		_uiSystem.OpenUi(ev.Performer, SpellsUiKey.Key, ev.Performer);
		UpdateSpellsUI((ev.Performer, cultistComp));
	}

	private void UpdateSpellsUI(Entity<BloodCultistComponent> entity)
	{
		if (_uiSystem.HasUi(entity, SpellsUiKey.Key))
			_uiSystem.SetUiState(entity.Owner, SpellsUiKey.Key, new BloodCultSpellsBuiState());
	}
	#endregion

	#region RuneEvents
	public void UseReviveRune(EntityUid target, EntityUid? user, EntityUid? used)
	{
		var attempt = new ReviveRuneAttemptEvent(target, user, used);
		RaiseLocalEvent(target, attempt, true);
	}

	public void UseGhostifyRune(EntityUid target, EntityUid? user, EntityUid used)
	{
		var attempt = new GhostifyRuneEvent(target, user, used);
		RaiseLocalEvent(target, attempt, true);
	}

	public void UseSacrificeRune(EntityUid target, EntityUid user, EntityUid used, EntityUid[] otherCultists)
	{
		var attempt = new SacrificeRuneEvent(target, user, used, otherCultists);
		RaiseLocalEvent(user, attempt, true);
	}

	public void UseConvertRune(EntityUid target, EntityUid user, EntityUid used, EntityUid[] otherCultists)
	{
		var attempt = new ConvertRuneEvent(target, user, used, otherCultists);
		RaiseLocalEvent(user, attempt, true);
	}
	#endregion
}
