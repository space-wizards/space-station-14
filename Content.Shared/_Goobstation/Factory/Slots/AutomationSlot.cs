// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Factory.Filters;
using Content.Shared.DeviceLinking;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Goobstation.Factory.Slots;

/// <summary>
/// An abstraction over some way to insert/take an item from a machine.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class AutomationSlot
{
    /// <summary>
    /// The input port for this slot, or null if can only be used as an output.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype>? Input;

    /// <summary>
    /// The output port for this slot, or null if can only be used as an input.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype>? Output;

    /// <summary>
    /// Whitelist that can be used in YML regardless of slot type.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist that can be used in YML regardless of slot type.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// The automated machine this slot belongs to.
    /// </summary>
    [ViewVariables]
    public EntityUid Owner;

    [Dependency] public readonly IEntityManager EntMan = default!;
    protected AutomationFilterSystem _filter;
    protected EntityWhitelistSystem _whitelist;
    protected SharedDeviceLinkSystem _device;

    /// <summary>
    /// Initialize the slot after <see cref="Owner"/> is set.
    /// System dependencies don't work so inheritors have to call <c>base.Initialize()</c> and then add their systems.
    /// </summary>
    public virtual void Initialize()
    {
        IoCManager.InjectDependencies(this);

        _filter = EntMan.System<AutomationFilterSystem>();
        _whitelist = EntMan.System<EntityWhitelistSystem>();
        _device = EntMan.System<SharedDeviceLinkSystem>();
    }

    /// <summary>
    /// Try to insert an item into the slot, returning true if it was removed from its previous container.
    /// Inheritors must override this and use <c>if (!base.Insert(uid, item)) return false;</c>
    /// </summary>
    public virtual bool Insert(EntityUid item)
    {
        return CanInsert(item);
    }

    /// <summary>
    /// Check if an item can be inserted into the slot, returning true if it can.
    /// Inheritors must override this and use <c>if (!base.CanInsert(uid, item)) return false;</c>
    /// </summary>
    public virtual bool CanInsert(EntityUid item)
    {
        return _whitelist.CheckBoth(item, whitelist: Whitelist, blacklist: Blacklist);
    }

    /// <summary>
    /// Get an item that can be taken from this slot, which has to match a given filter.
    /// If there are multiple items, which one returned is arbitrary and should not be relied upon.
    /// This should be "pure" and not actually modify anything.
    /// </summary>
    public virtual EntityUid? GetItem(EntityUid? filter)
    {
        return null;
    }

    /// <summary>
    /// Called to add all of this slot's ports to the machine.
    /// </summary>
    public virtual void AddPorts()
    {
        if (Input is {} input)
            _device.EnsureSinkPorts(Owner, input);
        if (Output is {} output)
            _device.EnsureSourcePorts(Owner, output);
    }

    /// <summary>
    /// Called to remove all of this slot's ports from the machine.
    /// </summary>
    public virtual void RemovePorts()
    {
        if (Input is {} input)
            _device.RemoveSinkPort(Owner, input);
        if (Output is {} output)
            _device.RemoveSourcePort(Owner, output);
    }
}
