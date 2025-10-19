// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Access;

[Serializable, NetSerializable]
public enum AccessWireActionKey : byte
{
    Key,
    Status,
    Pulsed,
    PulseCancel
}

[Serializable, NetSerializable]
public enum LogWireActionKey : byte
{
    Key,
    Status,
    Pulsed,
    PulseCancel
}
