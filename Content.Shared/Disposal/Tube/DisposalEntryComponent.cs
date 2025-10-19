// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Disposal.Unit;
using Robust.Shared.Prototypes;

namespace Content.Shared.Disposal.Tube;

[RegisterComponent]
[Access(typeof(SharedDisposalTubeSystem), typeof(SharedDisposalUnitSystem))]
public sealed partial class DisposalEntryComponent : Component
{
    [DataField]
    public EntProtoId HolderPrototypeId = "DisposalHolder";
}
