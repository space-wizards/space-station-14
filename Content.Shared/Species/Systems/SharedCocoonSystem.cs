// SPDX-FileCopyrightText: 2025 Drywink <hugogrethen@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Species.Arachnid;

public abstract class SharedCocoonSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CocoonerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CocoonerComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(EntityUid uid, CocoonerComponent component, MapInitEvent args)
    {
        // Check if the action prototype exists (test-safe)
        if (component.WrapAction != default && !_protoManager.TryIndex<EntityPrototype>(component.WrapAction, out _))
            return;

        _actions.AddAction(uid, ref component.ActionEntity, component.WrapAction, container: uid);
    }

    private void OnShutdown(EntityUid uid, CocoonerComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEntity);
    }
}

public sealed partial class WrapActionEvent : EntityTargetActionEvent
{
}

public sealed partial class UnwrapActionEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class WrapDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class UnwrapDoAfterEvent : SimpleDoAfterEvent
{
}
