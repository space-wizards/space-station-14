// SPDX-FileCopyrightText: 2025 Drywink <hugogrethen@gmail.com>
//
// SPDX-License-Identifier: MIT

using System;
using Content.Shared.Actions;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Species;

public abstract class SharedBiteSystem : EntitySystem
{
    [Dependency] protected readonly SharedActionsSystem Actions = default!;
    [Dependency] protected readonly IPrototypeManager ProtoManager = default!;
    [Dependency] protected readonly MetaDataSystem MetaDataSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BiteComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BiteComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(EntityUid uid, BiteComponent component, MapInitEvent args)
    {
        // Check if the action prototype exists (test-safe)
        if (component.BiteAction != default && !ProtoManager.TryIndex<EntityPrototype>(component.BiteAction, out _))
            return;

        Actions.AddAction(uid, ref component.ActionEntity, component.BiteAction, container: uid);

        // Set the cooldown from the component
        if (component.ActionEntity != null && component.Cooldown > 0)
        {
            Actions.SetUseDelay(component.ActionEntity, TimeSpan.FromSeconds(component.Cooldown));
        }

        // Set custom description if provided
        if (component.ActionEntity != null && component.ActionDescription != null)
        {
            MetaDataSystem.SetEntityDescription(component.ActionEntity.Value, Loc.GetString(component.ActionDescription));
        }
    }

    private void OnShutdown(EntityUid uid, BiteComponent component, ComponentShutdown args)
    {
        Actions.RemoveAction(uid, component.ActionEntity);
    }
}

/// <summary>
/// Event raised when an entity uses the bite ability on a target.
/// </summary>
public sealed partial class BiteActionEvent : EntityTargetActionEvent
{
}

