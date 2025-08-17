// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sandwich.ServerCurrency.UI
{
    [Serializable, NetSerializable]
    public sealed class CurrencyEuiState : EuiStateBase
    {

    }
    public static class CurrencyEuiMsg
    {
        [Serializable, NetSerializable]
        public sealed class Close : EuiMessageBase
        {
        }

        [Serializable, NetSerializable]
        public sealed class Buy : EuiMessageBase
        {
            public ProtoId<TokenListingPrototype> TokenId;
        }
    }
}