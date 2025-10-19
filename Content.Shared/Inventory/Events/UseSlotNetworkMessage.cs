// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Inventory.Events;

[NetSerializable, Serializable]
public sealed class UseSlotNetworkMessage : EntityEventArgs
{
    // The slot-owner is implicitly the client that is sending this message.
    // Otherwise clients could start forcefully undressing other clients.
    public readonly string Slot;

    public UseSlotNetworkMessage(string slot)
    {
        Slot = slot;
    }
}
