using Content.Server.Power.Components;
using Content.Shared.Audio;
using Content.Shared.Radio.Components;

namespace Content.Server.Radio.EntitySystems;

public sealed class TelecomServerSystem : EntitySystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelecomServerComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(EntityUid uid, TelecomServerComponent component, ref PowerChangedEvent args)
    {
        _ambient.SetAmbience(uid, args.Powered);
    }
}
