// SPDX-FileCopyrightText: 2025 AftrLite
// SPDX-FileCopyrightText: 2025 Janet Blackquill <uhhadd@gmail.com>
//
// SPDX-License-Identifier: LicenseRef-CosmicCult

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicRiftComponent : Component
{
    [DataField] public bool Used;

    [DataField] public bool Occupied;

    [DataField] public TimeSpan PurgeTime = TimeSpan.FromSeconds(35);

    [DataField] public TimeSpan AbsorbTime = TimeSpan.FromSeconds(25);
}
