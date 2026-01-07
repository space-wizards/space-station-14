// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.BloodCult.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class SoulStoneComponent : Component
    {
        /// <summary>
        /// The prototype ID of the original entity that was used to create this soulstone.
        /// When the soulstone breaks, it will revert to this entity type.
        /// </summary>
        [DataField]
        public EntProtoId? OriginalEntityPrototype;
    }
}
