using Content.Client.Gameplay;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.Medical;

public sealed class MedicalUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>,
    IOnSystemChanged<WoundSystem>
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [UISystemDependency] private readonly WoundSystem _woundSystem = default!;


    public void OnStateEntered(GameplayState state)
    {
    }

    public void OnStateExited(GameplayState state)
    {
    }

    public void OnSystemLoaded(WoundSystem system)
    {
    }

    public void OnSystemUnloaded(WoundSystem system)
    {
    }
}
