using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;
using Content.Shared.BloodCult.Prototypes;
using Content.Client.BloodCult.UI;

namespace Content.Client.BloodCult;

/// <summary>
/// Handles the spell selection radial menu for blood cultists
/// This allows cultists to select a spell to memorize from a radial menu
/// </summary>
public sealed class SpellsBoundUserInterface : BoundUserInterface
{
	[Dependency] private readonly IClyde _displayManager = default!;
	[Dependency] private readonly IInputManager _inputManager = default!;
	[Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

	private SpellRadialMenu? _spellRitualMenu;

	public SpellsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
	{
	}

	protected override void Open()
	{
		base.Open();

		_spellRitualMenu = this.CreateWindow<SpellRadialMenu>();
		_spellRitualMenu.InitializeDependencies(_entitySystemManager.DependencyCollection);
		_spellRitualMenu.SetEntity(Owner);
		_spellRitualMenu.SendSpellsMessageAction += SendSpellsMessage;

		var vpSize = _displayManager.ScreenSize;
		_spellRitualMenu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
	}

	private void SendSpellsMessage(ProtoId<CultAbilityPrototype> protoId)
	{
		SendPredictedMessage(new SpellsMessage(protoId));
	}
}