// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Goobstation.Factory;

/// <summary>
/// Makes a storage check filter slot and invoke signals.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(StorageBinSystem))]
public sealed partial class StorageBinComponent : Component
{
    /// <summary>
    /// Signal port invoked after inserting an item.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> InsertedPort = "StorageInserted";

    /// <summary>
    /// Signal port invoked after removing an item.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> RemovedPort = "StorageRemoved";
}

[Serializable, NetSerializable]
public enum StorageBinLayers : byte
{
    Powered
}
