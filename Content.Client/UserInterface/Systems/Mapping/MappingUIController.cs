using Content.Client.Gameplay;
using Content.Client.Hands.Systems;
using Content.Client.Inventory;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.Mapping;

public sealed class MappingUIController: UIController, IOnStateEntered<MappingState>, IOnStateExited<MappingState>
{
    public void OnStateEntered(MappingState state)
    {
        state.
    }

    public void OnStateExited(MappingState state)
    {
        throw new NotImplementedException();
    }
}
