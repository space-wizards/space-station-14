// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Gateway;

/// <summary>
/// Sent from client to server upon taking a gateway destination.
/// </summary>
[Serializable, NetSerializable]
public sealed class GatewayDestinationMessage : EntityEventArgs
{
    public int Index;
}
