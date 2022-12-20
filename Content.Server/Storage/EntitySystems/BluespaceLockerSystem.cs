using System.Linq;
using Content.Server.Lock;
using Content.Server.Mind.Components;
using Content.Server.Storage.Components;
using Content.Server.Tools.Systems;
using Microsoft.Extensions.DependencyModel;

namespace Content.Server.Storage.EntitySystems;

public sealed class BluespaceLockerSystem : EntitySystem
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly WeldableSystem _weldableSystem = default!;
    [Dependency] private readonly LockSystem _lockSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BluespaceLockerComponent, StorageBeforeOpenEvent>(PreOpen);
        SubscribeLocalEvent<BluespaceLockerComponent, StorageAfterCloseEvent>(PostClose);
    }


    private void PreOpen(EntityUid uid, BluespaceLockerComponent component, StorageBeforeOpenEvent args)
    {
        EntityStorageComponent? entityStorageComponent = null;

        if (component.BluespaceLinks is not { Count: > 0 })
            return;

        if (!Resolve(uid, ref entityStorageComponent))
            return;

        // Select target
        var targetContainerStorageComponent = component.BluespaceLinks.ToArray()[new Random().Next(0, component.BluespaceLinks.Count)];
        BluespaceLockerComponent? targetContainerBluespaceComponent = null;

        // Close target if it is open
        if (targetContainerStorageComponent.Open)
            _entityStorage.CloseStorage(targetContainerStorageComponent.Owner, targetContainerStorageComponent);

        // Apply bluespace effects if target is not a bluespace locker, otherwise let it handle it
        if (!Resolve(targetContainerStorageComponent.Owner, ref targetContainerBluespaceComponent, false) ||
            targetContainerBluespaceComponent.BluespaceLinks is not { Count: > 0 })
        {
            // Move contained items
            if (component.TransportEntities)
                foreach (var entity in targetContainerStorageComponent.Contents.ContainedEntities.ToArray())
                {
                    if (!component.AllowSentient && EntityManager.HasComponent<MindComponent>(entity))
                        continue;
                    entityStorageComponent.Contents.Insert(entity, EntityManager);
                }

            // Move contained air
            if (component.TransportGas)
            {
                entityStorageComponent.Air.CopyFromMutable(targetContainerStorageComponent.Air);
                targetContainerStorageComponent.Air.Clear();
            }
        }
    }

    private void PostClose(EntityUid uid, BluespaceLockerComponent component, StorageAfterCloseEvent args)
    {
        EntityStorageComponent? entityStorageComponent = null;

        if (component.BluespaceLinks is not { Count: > 0 })
            return;

        if (!Resolve(uid, ref entityStorageComponent))
            return;

        // Select target
        var targetContainerStorageComponent = component.BluespaceLinks.ToArray()[new Random().Next(0, component.BluespaceLinks.Count)];

        // Move contained items
        if (component.TransportEntities)
            foreach (var entity in entityStorageComponent.Contents.ContainedEntities.ToArray())
            {
                if (!component.AllowSentient && EntityManager.HasComponent<MindComponent>(entity))
                    continue;
                targetContainerStorageComponent.Contents.Insert(entity, EntityManager);
            }

        // Move contained air
        if (component.TransportGas)
        {
            targetContainerStorageComponent.Air.CopyFromMutable(entityStorageComponent.Air);
            entityStorageComponent.Air.Clear();
        }

        // Open and empty target
        if (targetContainerStorageComponent.Open)
        {
            _entityStorage.EmptyContents(targetContainerStorageComponent.Owner, targetContainerStorageComponent);
            _entityStorage.ReleaseGas(targetContainerStorageComponent.Owner, targetContainerStorageComponent);
        }
        else
        {
            if (targetContainerStorageComponent.IsWeldedShut)
            {
                // It gets bluespaced open...
                _weldableSystem.ForceWeldedState(targetContainerStorageComponent.Owner, false);
                if (targetContainerStorageComponent.IsWeldedShut)
                    targetContainerStorageComponent.IsWeldedShut = false;
            }
            LockComponent? lockComponent = null;
            if (Resolve(targetContainerStorageComponent.Owner, ref lockComponent, false) && lockComponent.Locked)
                _lockSystem.Unlock(lockComponent.Owner, lockComponent.Owner, lockComponent);

            _entityStorage.OpenStorage(targetContainerStorageComponent.Owner, targetContainerStorageComponent);
        }
    }
}
