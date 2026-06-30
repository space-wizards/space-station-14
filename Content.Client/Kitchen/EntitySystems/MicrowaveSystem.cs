using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.EntitySystems;

namespace Content.Client.Kitchen.EntitySystems;

/// <inheritdoc />
public sealed partial class MicrowaveSystem : SharedMicrowaveSystem
{
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<MicrowaveComponent>(MicrowaveUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>((ent, ref a) => UpdateUserInterfaceState(ent.AsNullable()));
        });
    }

    public override void UpdateUserInterfaceState(Entity<MicrowaveComponent?> microwave)
    {
        base.UpdateUserInterfaceState(microwave);

        if (!Resolve(microwave.Owner, ref microwave.Comp))
            return;

        if (_userInterface.TryGetOpenUi(microwave.Owner, MicrowaveUiKey.Key, out var bui))
            bui.Update();
    }
}
