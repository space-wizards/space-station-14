// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Materials;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared._Goobstation.Factory.Slots;

/// <summary>
/// Abstraction over inserting
/// Removing items is not supported.
/// </summary>
public sealed partial class AutomatedMaterialStorage : AutomationSlot
{
    private SharedMaterialStorageSystem _material;
    private SharedPowerReceiverSystem _power;

    private EntityQuery<MaterialComponent> _materialQuery;
    private EntityQuery<MaterialStorageComponent> _storageQuery;
    private EntityQuery<PhysicalCompositionComponent> _compositionQuery;

    public override void Initialize()
    {
        base.Initialize();

        _material = EntMan.System<SharedMaterialStorageSystem>();
        _power = EntMan.System<SharedPowerReceiverSystem>();

        _materialQuery = EntMan.GetEntityQuery<MaterialComponent>();
        _storageQuery = EntMan.GetEntityQuery<MaterialStorageComponent>();
        _compositionQuery = EntMan.GetEntityQuery<PhysicalCompositionComponent>();
    }

    public override bool Insert(EntityUid item)
    {
        return base.Insert(item) && _material.TryInsertMaterialEntity(user: Owner, item, Owner);
    }

    public override bool CanInsert(EntityUid item)
    {
        if (!base.CanInsert(item) || !_storageQuery.TryComp(Owner, out var storage))
            return false;

        // don't bypass power check for lathes and stuff
        if (!_power.IsPowered(Owner))
            return false;

        // this has to be essentially copypasted because goidacode doesnt have a CanInsertMaterial method
        if (!_materialQuery.HasComp(item) || !_compositionQuery.HasComp(item))
            return false;

        // not checking volume etc since all lathes currently have unlimited capacity
        return _whitelist.IsWhitelistPassOrNull(storage.Whitelist, item);
    }
}
