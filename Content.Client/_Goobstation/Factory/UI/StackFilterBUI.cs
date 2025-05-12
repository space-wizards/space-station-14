// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Factory.Filters;
using Robust.Client.UserInterface;

namespace Content.Client._Goobstation.Factory.UI;

public sealed class StackFilterBUI : BoundUserInterface
{
    private StackFilterWindow? _window;

    public StackFilterBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<StackFilterWindow>();
        _window.SetEntity(Owner);
        _window.OnSetMin += min => SendPredictedMessage(new StackFilterSetMinMessage(min));
        _window.OnSetSize += size => SendPredictedMessage(new StackFilterSetSizeMessage(size));
    }
}
