using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Tools.Systems;

public sealed class SliceableSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SliceableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SliceableComponent, SliceableDoafterEvent>(OnSliceDoAfter);
    }

    private void OnInteractUsing(Entity<SliceableComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (entity.Comp.SpawnedEntities.Count == 0)
            return;

        if (!_toolSystem.UseTool(args.Used, args.User, entity,  entity.Comp.DoafterTime.Seconds, entity.Comp.SlicingQuality, new SliceableDoafterEvent()))
            return;

        args.Handled = true;
    }

    private void OnSliceDoAfter(Entity<SliceableComponent> entity, ref SliceableDoafterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        SplitIntoSlices(entity, args.User);
        args.Handled = true;
    }

    private void SplitIntoSlices(Entity<SliceableComponent> entity, EntityUid user)
    {
        var mapCoords = _transform.GetMapCoordinates(entity);
        var sliceableXform = Transform(entity);

        if (_net.IsServer)
        {

            var spawnEntities = EntitySpawnCollection.GetSpawns(entity.Comp.SpawnedEntities, _random);

            foreach (var sliceProto in spawnEntities)
            {
                // distribute the spawned items randomly in a small radius around the origin
                var sliceUid = Spawn(sliceProto, mapCoords);

                // try putting the slice into the container if the food being sliced is in a container!
                // this lets you do things like slice a pizza up inside of a hot food cart without making a food-everywhere mess
                _transform.DropNextTo(sliceUid, (entity, sliceableXform));
                _transform.SetLocalRotation(sliceUid, 0);

                // small movement animation makes it look fancier
                if (!_container.IsEntityOrParentInContainer(sliceUid))
                {
                    var randVect = _random.NextVector2(entity.Comp.MinSpawnOffset, entity.Comp.MaxSpawnOffset);
                    if (TryComp<PhysicsComponent>(sliceUid, out var physics))
                        _physics.SetLinearVelocity(sliceUid, randVect, body: physics);
                }
            }
            QueueDel(entity);
        }

        _audio.PlayPredicted(entity.Comp.Sound, sliceableXform.Coordinates, user, AudioParams.Default.WithVolume(-2));
    }
}
