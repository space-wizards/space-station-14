// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.SMES;

[Serializable, NetSerializable]
public enum SmesVisuals
{
    LastChargeState,
    LastChargeLevel,
}
