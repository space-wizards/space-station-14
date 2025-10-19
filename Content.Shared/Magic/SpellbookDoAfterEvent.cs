// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Magic;

[Serializable, NetSerializable]
public sealed partial class SpellbookDoAfterEvent : SimpleDoAfterEvent
{
}
