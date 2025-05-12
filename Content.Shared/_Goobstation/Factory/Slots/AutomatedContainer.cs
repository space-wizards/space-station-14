// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Containers;

namespace Content.Shared._Goobstation.Factory.Slots;

/// <summary>
/// Abstraction over a <see cref="BaseContainer"/> on the machine.
/// </summary>
public sealed partial class AutomatedContainer : AutomationSlot
{
    /// <summary>
    /// The ID of the container to use.
    /// </summary>
    [DataField(required: true)]
    public string ContainerId = string.Empty;

    [DataField(required: true)]
    public int MaxItems;

    private SharedContainerSystem _container;

    public BaseContainer Container;

    public override void Initialize()
    {
        base.Initialize();

        _container = EntMan.System<SharedContainerSystem>();

        Container = _container.GetContainer(Owner, ContainerId);
    }

    public override bool Insert(EntityUid item)
    {
        return base.Insert(item) && _container.Insert(item, Container);
    }

    public override bool CanInsert(EntityUid item)
    {
        return base.CanInsert(item)
            && Container.Count < MaxItems
            && _container.CanInsert(item, Container);
    }

    public override EntityUid? GetItem(EntityUid? filter)
    {
        foreach (var item in Container.ContainedEntities)
        {
            if (_filter.IsAllowed(filter, item))
                return item;
        }

        return null;
    }
}
