// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Player;

namespace Content.Shared.Players;

/// <summary>
///     To be used from some systems.
///     Otherwise, use <see cref="ISharedPlayerManager"/>
/// </summary>
public abstract class SharedPlayerSystem : EntitySystem
{
    public abstract ContentPlayerData? ContentData(ICommonSession? session);
}
