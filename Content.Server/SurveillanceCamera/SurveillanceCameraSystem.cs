using Content.Shared.SurveillanceCamera;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraSystem : SharedSurveillanceCameraSystem
{
    [Dependency] private ViewSubscriberSystem _viewSubscriberSystem = default!;

    private void OnShutdown(EntityUid camera, SurveillanceCameraComponent component, ComponentShutdown args)
    {
        Deactivate(camera, component);
    }

    // If the camera deactivates for any reason, it must have all viewers removed,
    // and the relevant event broadcast to all systems.
    private void Deactivate(EntityUid camera, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component))
        {
            return;
        }

        RemoveActiveViewers(camera, new(component.ActiveViewers), component);
        RaiseLocalEvent(new SurveillanceCameraDeactivateEvent(camera));
    }

    public void AddActiveViewer(EntityUid camera, EntityUid player, SurveillanceCameraComponent? component = null, ActorComponent? actor = null)
    {
        if (!Resolve(camera, ref component)
            || !Resolve(player, ref actor))
        {
            return;
        }

        _viewSubscriberSystem.AddViewSubscriber(camera, actor.PlayerSession);
        component.ActiveViewers.Add(player);
    }

    public void AddActiveViewers(EntityUid camera, HashSet<EntityUid> players, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component))
        {
            return;
        }

        foreach (var player in players)
        {
            AddActiveViewer(camera, player, component);
        }
    }

    // Switch the set of active viewers from one camera to another.
    public void SwitchActiveViewers(EntityUid oldCamera, EntityUid newCamera, HashSet<EntityUid> players, SurveillanceCameraComponent? oldCameraComponent = null, SurveillanceCameraComponent? newCameraComponent = null)
    {
        if (!Resolve(oldCamera, ref oldCameraComponent)
            || !Resolve(newCamera, ref newCameraComponent))
        {
            return;
        }

        foreach (var player in players)
        {
            RemoveActiveViewer(oldCamera, player, oldCameraComponent);
            AddActiveViewer(newCamera, player, newCameraComponent);
        }
    }

    public void RemoveActiveViewer(EntityUid camera, EntityUid player, SurveillanceCameraComponent? component = null, ActorComponent? actor = null)
    {
        if (!Resolve(camera, ref component)
            || !Resolve(player, ref actor))
        {
            return;
        }

        _viewSubscriberSystem.RemoveViewSubscriber(camera, actor.PlayerSession);
        component.ActiveViewers.Remove(player);
    }

    public void RemoveActiveViewers(EntityUid camera, HashSet<EntityUid> players, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component))
        {
            return;
        }

        foreach (var player in players)
        {
            RemoveActiveViewer(camera, player, component);
        }
    }
}

public sealed class OnSurveillanceCameraViewerAddEvent : EntityEventArgs
{

}

public sealed class OnSurveillanceCameraViewerRemoveEvent : EntityEventArgs
{

}

// What happens when a camera deactivates.
public sealed class SurveillanceCameraDeactivateEvent : EntityEventArgs
{
    public EntityUid Camera { get; }

    public SurveillanceCameraDeactivateEvent(EntityUid camera)
    {
        Camera = camera;
    }
}
