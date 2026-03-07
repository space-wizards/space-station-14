using Content.Server.Revolutionary;
using Content.Server.Zombies;
using Content.Shared.Antag;
using Content.Shared.Mind.Components;

namespace Content.Sever.Antag;

public sealed class ShowAntagIconsSystem : EntitySystem
{
    [Dependency] private readonly ZombieSystem _zombie = default!;
    [Dependency] private readonly RevolutionarySystem _revolutionary = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShowAntagIconsComponent, ComponentStartup>((_, _, _) => DirtyAntagComps());
        SubscribeLocalEvent<ShowAntagIconsComponent, MindAddedMessage>((_, _, _) => DirtyAntagComps());
    }

    private void DirtyAntagComps()
    {
        // I hate this but this is the only way to handle both systems without making one a dependency of the other.
        // TODO: Make the API for session specific networking sane.
        _zombie.DirtyInitialInfectedComps();
        _revolutionary.DirtyRevComps();
    }
}
