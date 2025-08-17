// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Fishbait <Fishbait@git.ml>
// SPDX-FileCopyrightText: 2025 fishbait <gnesse@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Server._Imp.Drone
{
    [RegisterComponent]
    [AutoGenerateComponentPause]
    public sealed partial class DroneComponent : Component
    {
        public float InteractionBlockRange = 1.5f; /// imp. original value was 2.15, changed because it was annoying. this also does not actually block interactions anymore.

        // imp. delay before posting another proximity alert
        public TimeSpan ProximityDelay = TimeSpan.FromMilliseconds(2000);

        [AutoPausedField]
        public TimeSpan NextProximityAlert = new();

        public EntityUid NearestEnt = default!;

        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public EntityWhitelist? Whitelist;

        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public EntityWhitelist? Blacklist;
    }
}