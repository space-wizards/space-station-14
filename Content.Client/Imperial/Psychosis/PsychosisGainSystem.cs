using Content.Shared.Traits.Assorted;
using Robust.Shared.Random;
using Robust.Client.Player;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Content.Client.Chat.Managers;
using Content.Shared.Mobs.Components;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Content.Shared.Imperial.ICCVar;
namespace Content.Client.Psychosis;

public sealed class PsychosisGainSystem : SharedPsychosisGainSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();
        //SubscribeLocalEvent<PsychosisGainComponent, ComponentStartup>(OnComponentStartup);
        //SubscribeLocalEvent<PsychosisGainComponent, PlayerDetachedEvent>(OnPlayerDetach);
        SubscribeNetworkEvent<Stats>(Stats);
    }
    private void Stats(Stats psychosi, EntitySessionEventArgs args)
    {
        if (!TryComp<PsychosisGainComponent>(GetEntity(psychosi.PsychosisGain), out var psych))
            return;
        psych.Resist = psychosi.Resist;
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;
        if (_cfg.GetCVar(ICCVars.PsychosisEnabled) == true)
        {
            if (_player.LocalPlayer?.ControlledEntity is not EntityUid localPlayer)
                return;
            Checks(localPlayer);
        }
    }
    private void Checks(EntityUid uid)
    {
        if (_cfg.GetCVar(ICCVars.PsychosisEnabled) == false)
            return;
        if (!TryComp<PsychosisGainComponent>(uid, out var component))
            return;
        if (TryComp<PsychosisComponent>(uid, out var psychosis))
            return;
        if (component.NextUpdate > _timing.CurTime)
            return;
        component.NextUpdate = _timing.CurTime + component.Cooldown;
        var resist = component.Resist;
        var query = EntityQueryEnumerator<PsychosisGainComponent>();
        var prefList = new List<EntityUid>();
        var b = _eyeManager.GetWorldViewbounds();
        while (query.MoveNext(out var uidhuman, out var gainComponent))
        {
            if (!TryComp<MobStateComponent>(uidhuman, out var state))
                return;
            var cords = Transform(uidhuman).WorldPosition;
            if (!b.Contains(cords))
                return;
            if (state.CurrentState == Shared.Mobs.MobState.Dead)
                prefList.Add(uidhuman);
        }
        var found = false;
        foreach (var uidhuman in prefList)
        {
            var cords = Transform(uidhuman);
            var x1 = cords.Coordinates.X;
            var y1 = cords.Coordinates.Y;
            var cordsplayer = Transform(uid);
            var x2 = cordsplayer.Coordinates.X;
            var y2 = cordsplayer.Coordinates.Y;
            var distanceX = x1 - x2;
            var distanceY = y1 - y2;
            if (distanceX < 0)
            {
                distanceX *= -1;
            }
            if (distanceY < 0)
            {
                distanceY *= -1;
            }
            var distance = distanceX + distanceY;
            if (distance <= 4)
            {
                found = true;
            }
        }
        if (found)
        {
            component.Status += (_random.Next(2, 7) * (1f - component.Resist));
            if (component.Status >= 100)
            {
                component.Status = 0f;
                var ev = new Stats(true, component.Resist, component.Status, GetNetEntity(uid));
                RaiseNetworkEvent(ev);
                Dirty(component);
            }
        }
        else
        {
            if (component.Status != 0)
                component.Status -= 1;
            Dirty(component);
        }
    }
}
