


using Content.Client.Chemistry.Components;
using Content.Client.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Client.Chemistry.EntitySystems;

public sealed class ClientMedipenRefillerSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedipenRefillerComponent, GotEmaggedEvent>(OnGotEmaggedEvent);
    }


    private void OnGotEmaggedEvent(EntityUid uid, MedipenRefillerComponent component, ref GotEmaggedEvent args)
    {
        if (!Resolve(uid, ref component!) || _entityManager.HasComponent<EmaggedComponent>(uid))
            return;

        _audio.PlayPredicted(component.SparkSound, uid, args.UserUid);
        args.Handled = true;
    }
}
