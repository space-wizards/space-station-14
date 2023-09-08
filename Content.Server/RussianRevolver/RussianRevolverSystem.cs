using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server.RussianRevolver;

public sealed class RussianRevolverSystem : EntitySystem
{
    [Dependency]
    private readonly SharedGunSystem _gunSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RussianRevolverComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, RussianRevolverComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out RevolverAmmoProviderComponent? ammoDrinker))
        {
            return;
        }
        _gunSystem.RussianizeRevolver(uid, ammoDrinker);
    }
}
