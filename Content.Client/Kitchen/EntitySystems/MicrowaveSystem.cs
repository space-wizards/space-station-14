using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.EntitySystems;

namespace Content.Client.Kitchen.EntitySystems;

/// <inheritdoc />
public abstract partial class MicrowaveSystem : SharedMicrowaveSystem
{
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    public override void UpdateUserInterfaceState(Entity<MicrowaveComponent?> microwave)
    {
        base.UpdateUserInterfaceState(microwave);

        if (!Resolve(microwave.Owner, ref microwave.Comp))
            return;

        if (_userInterface.TryGetOpenUi(microwave.Owner, MicrowaveUiKey.Key, out var bui))
            bui.Update();
    }
}
