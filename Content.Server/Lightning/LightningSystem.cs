using Content.Server.Lightning.Components;
using Content.Shared.Interaction;
using Content.Shared.Lightning;

namespace Content.Server.Lightning;

public sealed class LightningSystem : SharedLightningSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightningComponent, InteractHandEvent>(OnHandInteract);
    }

    private void OnHandInteract(EntityUid uid, LightningComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        SpawnLightning(component);

        args.Handled = true;
    }
}
