using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.DeadSpace.Ports.Jukebox;

public sealed class JukeboxSharedSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhiteJukeboxComponent, ComponentStartup>(OnJukeboxInit);
    }

    public void OnJukeboxInit(EntityUid uid, WhiteJukeboxComponent component, ComponentStartup args)
    {
        component.TapeContainer =
            _containerSystem.EnsureContainer<Container>(uid, WhiteJukeboxComponent.JukeboxContainerName);

        component.DefaultSongsContainer =
            _containerSystem.EnsureContainer<Container>(uid, WhiteJukeboxComponent.JukeboxDefaultSongsName);

        if (_netManager.IsServer)
        {
            var transform = Transform(component.Owner);

            foreach (var tapePrototype in component.DefaultTapes)
            {
                var tapeUid = EntityManager.SpawnEntity(tapePrototype, transform.MapPosition);

                if (!TryComp<TapeComponent>(tapeUid, out _))
                    continue;

                _containerSystem.Insert(tapeUid, component.DefaultSongsContainer);
            }
        }
    }
}
