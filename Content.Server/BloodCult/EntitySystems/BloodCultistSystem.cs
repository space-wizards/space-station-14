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
		if (!TryComp<BloodCultistComponent>(ev.Performer, out var cultistComp))
			return;

		if (!_uiSystem.HasUi(ev.Performer, BloodCultistCommuneUIKey.Key))
			return;

		if (_uiSystem.IsUiOpen(ev.Performer, BloodCultistCommuneUIKey.Key))
			return;

		if (_uiSystem.TryOpenUi(ev.Performer, BloodCultistCommuneUIKey.Key, ev.Performer))
		{
			UpdateCommuneUI((ev.Performer, cultistComp));
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
}
