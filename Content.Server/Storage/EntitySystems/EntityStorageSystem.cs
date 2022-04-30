using System.Linq;
using Content.Server.Storage.Components;
using Content.Shared.Destructible;
using Robust.Shared.Physics;

namespace Content.Server.Storage.EntitySystems;

public sealed class EntityStorageSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntityStorageComponent, DestructionEventArgs>(OnDestroy);
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
