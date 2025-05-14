// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;

namespace Content.Shared._Goobstation.Factory.Filters;

/// <summary>
/// Filter that combines 2 other filters using a logical operation.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AutomationFilterSystem))]
[AutoGenerateComponentState]
public sealed partial class CombinedFilterComponent : Component
{
    /// <summary>
    /// Name of the first filter slot.
    /// </summary>
    public const string FilterAName = "combined_filter_a";

    /// <summary>
    /// Name of the second filter slot.
    /// </summary>
    public const string FilterBName = "combined_filter_b";

    /// <summary>
    /// The slot for the first filter.
    /// </summary>
    [ViewVariables]
    public ItemSlot FilterA = default!;

    /// <summary>
    /// The slot for the second filter.
    /// </summary>
    [ViewVariables]
    public ItemSlot FilterB = default!;

    /// <summary>
    /// Logic gate operation to check the inputs with.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LogicGate Gate = LogicGate.Or;
}
