// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using System.Linq;
using Content.Server.Mind;
using Content.Shared.BloodCult.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Speech;
using Content.Shared.Emoting;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.BloodCult.EntitySystems;

public sealed class JuggernautBodyContainerSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<JuggernautBodyContainerComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(EntityUid uid, JuggernautBodyContainerComponent component, MobStateChangedEvent args)
    {
        // When the juggernaut goes critical or dies, eject the body
        if (args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead)
        {
            EjectBody(uid, component);
        }

        // When the juggernaut dies, stop blocking projectiles and eject soulstone
        if (args.NewMobState == MobState.Dead)
        {
            DisableProjectileCollision(uid);
            EjectSoulstone(uid);
        }
    }

    private void EjectBody(EntityUid uid, JuggernautBodyContainerComponent component)
    {
        if (!_container.TryGetContainer(uid, component.ContainerId, out var container))
            return;

        var coordinates = Transform(uid).Coordinates;
        
        // Get the juggernaut's mind before ejecting
        EntityUid? juggernautMindId = CompOrNull<MindContainerComponent>(uid)?.Mind;
        MindComponent? juggernautMindComp = CompOrNull<MindComponent>(juggernautMindId);
        
        // Eject all entities from the container (should just be the body)
        foreach (var contained in container.ContainedEntities.ToArray())
        {
            _container.Remove(contained, container, destination: coordinates);
            
            // Give the body a physics push for visual effect
            if (TryComp<PhysicsComponent>(contained, out var physics))
            {
                // Wake the physics body so it responds to the impulse
                _physics.SetAwake((contained, physics), true);
                
                // Generate a random direction and speed (8-15 units/sec for dramatic ejection)
                var randomDirection = _random.NextVector2();
                var speed = _random.NextFloat(8f, 15f);
                var impulse = randomDirection * speed * physics.Mass;
                _physics.ApplyLinearImpulse(contained, impulse, body: physics);
            }
            
            // Transfer the mind back to the body
            if (juggernautMindId != null && juggernautMindComp != null)
            {
                _mind.TransferTo((EntityUid)juggernautMindId, contained, mind: juggernautMindComp);
            }
        }
    }

    private void DisableProjectileCollision(EntityUid uid)
    {
        // Disable collision so projectiles pass through dead juggernauts
        if (TryComp<PhysicsComponent>(uid, out var physics))
        {
            _physics.SetCanCollide(uid, false, body: physics);
        }
    }

    private void EjectSoulstone(EntityUid uid)
    {
        // Try to get the soulstone container
        if (!_container.TryGetContainer(uid, "juggernaut_soulstone_container", out var soulstoneContainer))
            return;

        // Check if there's actually a soulstone in the container
        if (soulstoneContainer.ContainedEntities.Count == 0)
            return;

        // Get the juggernaut's mind before ejecting
        EntityUid? mindId = CompOrNull<MindContainerComponent>(uid)?.Mind;
        if (mindId == null || !TryComp<MindComponent>(mindId, out var mindComp))
            return;

        var coordinates = Transform(uid).Coordinates;

        // Eject all entities from the soulstone container (should just be the soulstone)
        foreach (var contained in soulstoneContainer.ContainedEntities.ToArray())
        {
            var soulstone = contained;
            
            // Transfer the mind back to the soulstone
            _mind.TransferTo((EntityUid)mindId, soulstone, mind: mindComp);
            
            // Ensure the soulstone can speak but not move
            EnsureComp<SpeechComponent>(soulstone);
            EnsureComp<EmotingComponent>(soulstone);
            
            // Remove the soulstone from the container
            _container.Remove(soulstone, soulstoneContainer, destination: coordinates);
            
            // Give the soulstone a physics push for visual effect
            if (TryComp<PhysicsComponent>(soulstone, out var physics))
            {
                // Wake the physics body so it responds to the impulse
                _physics.SetAwake((soulstone, physics), true);
                
                // Generate a random direction and speed (8-15 units/sec for dramatic ejection)
                var randomDirection = _random.NextVector2();
                var speed = _random.NextFloat(8f, 15f);
                var impulse = randomDirection * speed * physics.Mass;
                _physics.ApplyLinearImpulse(soulstone, impulse, body: physics);
            }
        }
    }
}

