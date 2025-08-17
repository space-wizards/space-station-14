// SPDX-FileCopyrightText: 2024 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared._Sandwich.ServerCurrency;

/// <summary>
///     Arguments for when a player's currency is changed.
/// </summary>
[ByRefEvent]
public readonly record struct PlayerBalanceChangeEvent
{
    /// <summary>
    ///     Creates a new instance of this class.
    /// </summary>
    public PlayerBalanceChangeEvent(ICommonSession userSes, NetUserId userId, int newBalance, int oldBalance)
    {
        UserSes = userSes;
        UserID = userId;
        NewBalance = newBalance;
        OldBalance = oldBalance;
    }

    /// <summary>
    ///     ICommonSession of the player with the balance change.
    /// </summary>
    public readonly ICommonSession UserSes;

    /// <summary>
    ///     NetUserId of the player with the balance change.
    /// </summary>
    public readonly NetUserId UserID;

    /// <summary>
    ///     New amount that replaced the old one.
    /// </summary>
    public readonly int NewBalance;

    /// <summary>
    ///     Old amount that was replaced.
    /// </summary>
    public readonly int OldBalance;
}

[Serializable, NetSerializable]
public sealed class PlayerBalanceUpdateEvent : EntityEventArgs
{
    /// <summary>
    ///     Creates a new instance of this class.
    /// </summary>
    public PlayerBalanceUpdateEvent(int newBalance, int oldBalance)
    {
        NewBalance = newBalance;
        OldBalance = oldBalance;
    }

    /// <summary>
    ///     New amount that replaced the old one.
    /// </summary>
    public int NewBalance;

    /// <summary>
    ///     Old amount that was replaced.
    /// </summary>
    public int OldBalance;

}

[Serializable, NetSerializable]
public sealed class PlayerBalanceRequestEvent : EntityEventArgs
{
    /// <summary>
    ///     Creates a new instance of this class.
    /// </summary>
    public PlayerBalanceRequestEvent(){}
}
