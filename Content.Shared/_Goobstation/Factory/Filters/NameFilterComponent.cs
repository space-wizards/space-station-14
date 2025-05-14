// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Goobstation.Factory.Filters;

/// <summary>
/// A filter that requires items to have the exact same name as a set string.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AutomationFilterSystem))]
[AutoGenerateComponentState]
public sealed partial class NameFilterComponent : Component
{
    /// <summary>
    /// The string to compare to the item name.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Name = string.Empty;

    /// <summary>
    /// Max length for <see cref="Name"/>.
    /// </summary>
    [DataField]
    public int MaxLength = 50;

    /// <summary>
    /// The filtering mode to use with <see cref="Name"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NameFilterMode Mode = NameFilterMode.Contain;
}

[Serializable, NetSerializable]
public enum NameFilterMode : byte
{
    // Name must contain a string somewhere
    Contain,
    // Name must start with a string
    Start,
    // Name must end with a string
    End,
    // Name must match exactly, even if it's labelled
    Match
}

[Serializable, NetSerializable]
public enum NameFilterUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed partial class NameFilterSetNameMessage(string name) : BoundUserInterfaceMessage
{
    public readonly string Name = name;
}

[Serializable, NetSerializable]
public sealed partial class NameFilterSetModeMessage(NameFilterMode mode) : BoundUserInterfaceMessage
{
    public readonly NameFilterMode Mode = mode;
}
