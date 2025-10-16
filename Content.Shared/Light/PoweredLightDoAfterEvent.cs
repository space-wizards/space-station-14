// SPDX-License-Identifier: MIT

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Light;

[Serializable, NetSerializable]
public sealed partial class PoweredLightDoAfterEvent : SimpleDoAfterEvent
{
}
