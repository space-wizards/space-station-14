// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Network;

namespace Content.Shared.Administration.Logs;

/// <summary>
/// Interface implemented by admin log values that contain player references.
/// </summary>
public interface IAdminLogsPlayerValue
{
    IEnumerable<NetUserId> Players { get; }
}
