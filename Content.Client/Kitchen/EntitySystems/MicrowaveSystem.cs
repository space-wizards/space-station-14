using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.EntitySystems;

namespace Content.Client.Kitchen.EntitySystems;

/// <inheritdoc />
public sealed partial class MicrowaveSystem : SharedMicrowaveSystem
{
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    public override void UpdateUI(Entity<MicrowaveComponent?> microwave)
    {
        base.UpdateUI(microwave);

        if (!Resolve(microwave.Owner, ref microwave.Comp))
            return;

        if (_userInterface.TryGetOpenUi(microwave.Owner, MicrowaveUiKey.Key, out var bui))
            bui.Update();
    }
}
