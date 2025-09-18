using System.Numerics;
using Content.Server.Emp;
using Content.Shared.Camera;
using Content.Shared._Starlight.Antags.Actions;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Server.Audio;
using Content.Shared.Actions;
using Content.Shared.Charges.Systems;

namespace Content.Server._Starlight.Antags.Actions;

public sealed partial class EMPScreamSystem : EntitySystem
{
    [Dependency] private readonly SharedChargesSystem _chargesSystem = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EMPScreamEvent>(OnEMPScream);

        base.Initialize();
    }

    private void OnEMPScream(EMPScreamEvent ev)
    {
        if (ev.Handled || _chargesSystem.GetCurrentCharges(ev.Action.Owner) == 0)
            return;

        ev.Handled = true;
        var uid = ev.Performer;

        ScreamEffect(uid, ev.Power, ev.ScreamSound);

        var pos = _transform.GetMapCoordinates(uid);
        _emp.EmpPulse(pos, ev.Power, ev.EnergyConsumption, ev.Power * ev.DurationMultiply);
    }

    private void ScreamEffect(EntityUid source, float screamPower, SoundSpecifier? sound = null)
    {
        if (sound != null)
            _audio.PlayPvs(sound, source);

        // Camera kick for all players in range.
        var center = _transform.GetMapCoordinates(source);
        var targets = Filter.Empty();
        targets.AddInRange(center, screamPower, _player, EntityManager);

        foreach (var target in targets.Recipients)
        {
            if (target.AttachedEntity == null)
                continue;

            var pos = _transform.GetWorldPosition(target.AttachedEntity!.Value);
            var delta = center.Position - pos;

            if (delta.EqualsApprox(Vector2.Zero))
                delta = new(.01f, 0);

            _recoil.KickCamera(source, -delta.Normalized());
        }
    }
}