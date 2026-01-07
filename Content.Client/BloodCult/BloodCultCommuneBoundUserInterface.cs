// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later or MIT

using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Content.Shared.BloodCult;

namespace Content.Client.BloodCult;

public sealed class BloodCultCommuneBoundUserInterface : BoundUserInterface
{
    //[Dependency] private readonly IPrototypeManager _protomanager = default!;

    [ViewVariables]
    private BloodCultCommuneWindow? _window;

    public BloodCultCommuneBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BloodCultCommuneWindow>();
        _window.OnCommune += OnCommuneSent;
    }

    private void OnCommuneSent(string message)
    {
		if (message.Length > 0)
		{
			SendMessage(new BloodCultCommuneSendMessage(message));
			_window?.Close();
		}
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not BloodCultCommuneBuiState cast || _window == null)
        {
            return;
        }

        _window.UpdateState(cast.Message);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
