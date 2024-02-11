using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;

namespace Content.Client.Ninja.Systems;

/// <summary>
/// Disables cloak prediction since client has no knowledge of battery power.
/// Cloak will still be enabled after server tells it.
/// </summary>
public sealed class NinjaSuitSystem : SharedNinjaSuitSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaSuitComponent, AttemptStealthEvent>(OnAttemptStealth);
    }

    private void OnAttemptStealth(EntityUid uid, NinjaSuitComponent comp, AttemptStealthEvent args)
    {
        args.Cancel();
    }
}
