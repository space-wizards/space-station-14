using System.Linq;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Server.Tools.Systems;
using Content.Shared.Destructible;
using Robust.Shared.Physics;
using Robust.Shared.Player;

namespace Content.Server.Storage.EntitySystems;

public sealed class EntityStorageSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public const string ContainerName = "entity_storage";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntityStorageComponent, WeldableAttemptEvent>(OnWeldableAttempt);
        SubscribeLocalEvent<EntityStorageComponent, WeldableChangedEvent>(OnWelded);
        SubscribeLocalEvent<EntityStorageComponent, DestructionEventArgs>(OnDestroy);
    }

    private void OnWeldableAttempt(EntityUid uid, EntityStorageComponent component, WeldableAttemptEvent args)
    {
        if (component.Open)
        {
            args.Cancel();
            return;
        }

        if (component.Contents.Contains(args.User))
        {
            var msg = Loc.GetString("entity-storage-component-already-contains-user-message");
            _popupSystem.PopupEntity(msg, args.User, Filter.Entities(args.User));
            args.Cancel();
        }
    }

    private void OnWelded(EntityUid uid, EntityStorageComponent component, WeldableChangedEvent args)
    {
        component.IsWeldedShut = args.IsWelded;
    }

    private void OnDestroy(EntityUid uid, EntityStorageComponent component, DestructionEventArgs args)
    {
        component.Open = true;
        EmptyContents(uid, component);
    }

    public void EmptyContents(EntityUid uid, EntityStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var containedArr = component.Contents.ContainedEntities.ToArray();
        foreach (var contained in containedArr)
        {
            if (component.Contents.Remove(contained))
            {
                Transform(contained).WorldPosition = component.ContentsDumpPosition();
                if (TryComp(contained, out IPhysBody? physics))
                {
                    physics.CanCollide = true;
                }
            }
        }
    }
}
