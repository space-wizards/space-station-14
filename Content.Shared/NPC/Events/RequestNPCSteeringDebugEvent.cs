// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.NPC.Events;

/// <summary>
/// Raised from client to server to request NPC steering debug info.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestNPCSteeringDebugEvent : EntityEventArgs
{
    public bool Enabled;
}
