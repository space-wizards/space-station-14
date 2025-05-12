// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Goobstation.Factory.Filters;

/// <summary>
/// A filter that requires items to have the exact same label as a set string.
/// Items without a label will always fail it.
/// Set labels using a hand labeler.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AutomationFilterSystem))]
[AutoGenerateComponentState]
public sealed partial class LabelFilterComponent : Component
{
    /// <summary>
    /// The label to require.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Label = string.Empty;

    /// <summary>
    /// Max length for <see cref="Label"/>.
    /// </summary>
    [DataField]
    public int MaxLength = 50;
}

[Serializable, NetSerializable]
public enum LabelFilterUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed partial class LabelFilterSetLabelMessage(string label) : BoundUserInterfaceMessage
{
    public readonly string Label = label;
}
