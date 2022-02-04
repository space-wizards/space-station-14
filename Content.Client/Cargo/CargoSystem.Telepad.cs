using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Cargo;

public sealed partial class CargoSystem
{
    private void InitializeCargoTelepad()
    {
        SubscribeLocalEvent<CargoTelepadComponent, AnimationCompletedEvent>(OnCargoAnimComplete);
    }

    private void OnCargoAnimComplete(EntityUid uid, CargoTelepadComponent component, AnimationCompletedEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance)) return;

        OnChangeData(appearance);
    }
}
