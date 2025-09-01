using System;
using System.Linq;
using Content.Server.Administration.Systems;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Tag;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Starlight.Medical.Surgery.Steps.Parts;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Content.Shared.Starlight.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
public abstract partial class SharedSurgerySystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly RotateToFaceSystem _rotateToFace = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly StarlightEntitySystem _entitySystem = default!;

    public override void Initialize()
    {
        base.Initialize();


        InitializeSteps();
        InitializeConditions();
    }

    public bool IsSurgeryValid
        (
            EntityUid body,
            EntityUid targetPart,
            EntProtoId surgery,
            EntProtoId stepId,
            out Entity<SurgeryComponent> surgeryEnt,
            out Entity<BodyPartComponent> partEnt,
            out EntityUid step
        )
    {
        surgeryEnt = default;
        partEnt = default;
        step = default;

        if (!HasComp<SurgeryTargetComponent>(body) 
             || !IsLyingDown(body) 
             || !_entitySystem.TryEntity(targetPart, out partEnt) 
             || !_entitySystem.TryGetSingleton(surgery, out var surgeryEntId) 
             || !_entitySystem.TryEntity(surgeryEntId, out surgeryEnt) 
             || !_entitySystem.TryGetSingleton(stepId, out step)
             || !surgeryEnt.Comp.Steps.Contains(stepId))
            return false;

        var progress = EnsureComp<SurgeryProgressComponent>(targetPart);

        var ev = new SurgeryValidEvent(body, targetPart);

        if (!progress.StartedSurgeries.Contains(surgery))
        {
            RaiseLocalEvent(step, ref ev);
            RaiseLocalEvent(surgeryEntId, ref ev);
        }

        return !ev.Cancelled;
    }

    protected List<EntityUid> GetTools(EntityUid surgeon) => [.. _hands.EnumerateHeld(surgeon)];

    public bool IsLyingDown(EntityUid entity)
    {
        if (_standing.IsDown(entity))
            return true;
        
        if (HasComp<ItemComponent>(entity))
            return true;

        if (TryComp(entity, out BuckleComponent? buckle) &&
            TryComp(buckle.BuckledTo, out StrapComponent? strap))
        {
            var rotation = strap.Rotation;
            if (rotation.GetCardinalDir() is Direction.West or Direction.East)
                return true;
        }

        return false;
    }

    protected virtual void RefreshUI(EntityUid body)
    {
    }
}
