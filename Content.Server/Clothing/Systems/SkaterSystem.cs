using Robust.Shared.Random;
using Content.Shared.Movement.Events;
using Content.Shared.Stunnable;
using Content.Shared.Clothing;

namespace Content.Server.Clothing;

public sealed class SkaterSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkaterComponent, MoveInputEvent>(OnMove);
    }

    public void OnMove(EntityUid uid, SkaterComponent component, ref MoveInputEvent args)
    {
        if (_robustRandom.Prob(component.KnockChance))
        {
            _audio.PlayPvs(component.KnockSound, uid);
            _stunSystem.TryParalyze(args.Entity, TimeSpan.FromSeconds(component.ParalyzeTime), true);
        }
    }
}
