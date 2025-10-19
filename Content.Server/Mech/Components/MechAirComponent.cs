// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Server.Atmos;
using Content.Shared.Atmos;

namespace Content.Server.Mech.Components;

[RegisterComponent]
public sealed partial class MechAirComponent : Component
{
    //TODO: this doesn't support a tank implant for mechs or anything like that
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public GasMixture Air = new (GasMixVolume);

    public const float GasMixVolume = 70f;
}
