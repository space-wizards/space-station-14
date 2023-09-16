using Robust.Shared.Random;
using Content.Shared.Movement.Events;
using Content.Shared.Stunnable;
using Content.Shared.Clothing;
using Content.Shared.Movement.Systems;

namespace Content.Server.Clothing;

public sealed class SkaterSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkaterComponent, MoveInputEvent>(OnMove);
    }

    public void OnMove(EntityUid uid, SkaterComponent component, ref MoveInputEvent args)
    {

        if (_random.Prob(component.KnockChance))
        {
            _audio.PlayPvs(component.KnockSound, uid);
            _stun.TryParalyze(args.Entity, TimeSpan.FromSeconds(component.ParalyzeTime), true);
        }
    }
}
