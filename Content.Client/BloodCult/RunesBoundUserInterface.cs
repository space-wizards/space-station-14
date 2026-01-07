// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2026 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Client.BloodCult.UI;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Shared.BloodCult.Components;

namespace Content.Client.BloodCult;

public sealed class RunesBoundUserInterface : BoundUserInterface
{
	[Dependency] private readonly IClyde _displayManager = default!;
	[Dependency] private readonly IInputManager _inputManager = default!;
	[Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

	private RuneRadialMenu? _runeRitualMenu;

	public RunesBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
	{
	}

	protected override void Open()
	{
		base.Open();

		_runeRitualMenu = this.CreateWindow<RuneRadialMenu>();
		_runeRitualMenu.InitializeDependencies(_entitySystemManager.DependencyCollection);
		_runeRitualMenu.SetEntity(Owner);
		_runeRitualMenu.SendRunesMessageAction += SendRunesMessage;//SendHereticRitualMessage;

		var vpSize = _displayManager.ScreenSize;
		_runeRitualMenu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
	}

	private void SendRunesMessage(string protoId)
	{
		SendMessage(new RunesMessage(protoId));
	}
}