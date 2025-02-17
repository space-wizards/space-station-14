// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Robust.Shared.Player;
using Content.Shared.Verbs;
using Content.Server.Administration.Managers;
using Robust.Shared.Utility;
using Content.Shared.Database;
using Robust.Shared.Timing;
using Content.Server.Chat.Systems;
using Content.Shared.DeadSpace.Necromorphs.Necroobelisk;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Server.Beam;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Systems;
using Content.Shared.Audio;

namespace Content.Server.DeadSpace.Necromorphs.Necroobelisk;

public sealed class NecroobeliskSystem : SharedNecroobeliskSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly BeamSystem _beam = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NecroobeliskComponent, GetVerbsEvent<Verb>>(DoSetObeliskVerbs);
        SubscribeLocalEvent<NecroobeliskComponent, SanityLostEvent>(OnSanityLost);
        SubscribeLocalEvent<NecroobeliskComponent, NecroobeliskPulseEvent>(OnSeverityChanged);
        SubscribeLocalEvent<NecroobeliskComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NecroobeliskComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<NecroobeliskComponent, NecroobeliskStartConvergenceEvent>(OnConvergence);
        SubscribeLocalEvent<NecroobeliskComponent, NecroobeliskAbsorbEvent>(DoAbsorb);
        SubscribeLocalEvent<NecroobeliskComponent, NecroMoonAppearanceEvent>(DoAppearanceMoon);
    }

    private void DoAbsorb(EntityUid uid, NecroobeliskComponent component, NecroobeliskAbsorbEvent args)
    {
        _beam.TryCreateBeam(uid, args.Target, "NecroLightning");

        QueueDel(args.Target);
        component.MobsAbsorbed += 1;
        Dirty(uid, component);
    }

    private void DoAppearanceMoon(EntityUid uid, NecroobeliskComponent component, NecroMoonAppearanceEvent args)
    {
        var ev = new EndStageConvergenceEvent();
        var query = AllEntityQuery<UnitologyRuleComponent>();
        while (query.MoveNext(out var rule, out _))
        {
            RaiseLocalEvent(rule, ref ev);
        }

        Spawn("NecroMoon", Transform(uid).Coordinates);
    }

    private void OnConvergence(EntityUid uid, NecroobeliskComponent component, NecroobeliskStartConvergenceEvent args)
    {
        if (!component.IsCanStartConvergence && !component.IsActive)
            return;

        var msg = new GameGlobalSoundEvent(component.SoundConvergence, AudioParams.Default);
        var stationFilter = _stationSystem.GetInOwningStation(uid);
        stationFilter.AddPlayersByPvs(uid, entityManager: EntityManager);
        RaiseNetworkEvent(msg, stationFilter);

        component.IsStageConvergence = true;
    }

    private void OnDestruction(EntityUid uid, NecroobeliskComponent component, DestructionEventArgs args)
    {
        GlobalWarn(uid, component, "uni-centcomm-announcement-obelisk-was-destroyed", Color.Green);
    }

    private void OnMapInit(EntityUid uid, NecroobeliskComponent component, MapInitEvent args)
    {
        component.NextPulseTime = _gameTiming.CurTime + component.TimeUtilPulse;
        GlobalWarn(uid, component, "uni-centcomm-announcement-obelisk-was-spawned", Color.Red);
        if (component.SpawnCudzu)
            Spawn("NecroKudzu", Transform(uid).Coordinates);
    }

    private void GlobalWarn(EntityUid uid, NecroobeliskComponent component, string str, Color color)
    {
        if (!component.IsGivesWarnings)
            return;

        var msg = new GameGlobalSoundEvent(component.SoundInit, AudioParams.Default);
        var stationFilter = _stationSystem.GetInOwningStation(uid);
        stationFilter.AddPlayersByPvs(uid, entityManager: EntityManager);
        RaiseNetworkEvent(msg, stationFilter);

        Timer.Spawn(20000,
        () => _chatSystem.DispatchGlobalAnnouncement(Loc.GetString(str), playSound: true, colorOverride: color));
    }
    private void OnSeverityChanged(EntityUid uid, NecroobeliskComponent component, ref NecroobeliskPulseEvent args)
    {
        if (_mobState.IsDead(uid))
            return;
    }

    private void OnSanityLost(EntityUid uid, NecroobeliskComponent component, ref SanityLostEvent args)
    {
        if (HasComp<NecromorfComponent>(args.VictinUID))
            return;

        if (!HasComp<InfectionDeadComponent>(args.VictinUID))
            AddComp<InfectionDeadComponent>(args.VictinUID);

        DamageSpecifier dspec = new();
        dspec.DamageDict.Add("Cellular", 2f);
        _damage.TryChangeDamage(args.VictinUID, dspec, true, false);
    }

    private void DoSetObeliskVerbs(EntityUid uid, NecroobeliskComponent component, GetVerbsEvent<Verb> args)
    {

        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.IsAdmin(player))
            return;

        if (component.IsActive)
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("Выключить обелиск"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
                Act = () => ToggleObeliskActive(uid, component),
                Impact = LogImpact.Medium
            });
        }
        else
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("Включить обелиск"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
                Act = () => ToggleObeliskActive(uid, component),
                Impact = LogImpact.Medium
            });
        }

        if (HasComp<DamageableComponent>(uid))
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("Включить неуязвимость"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
                Act = () => SetDamageable(uid, component),
                Impact = LogImpact.Medium
            });
        }
        else
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("Выключить неуязвимость"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
                Act = () => SetDamageable(uid, component),
                Impact = LogImpact.Medium
            });
        }
        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("Изменить радиус безумного фона на 10"),
            Category = VerbCategory.Debug,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
            Act = () => SetRangeSanity(component, 10f),
            Impact = LogImpact.Medium
        });
        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("Изменить радиус безумного фона на 20"),
            Category = VerbCategory.Debug,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
            Act = () => SetRangeSanity(component, 20f),
            Impact = LogImpact.Medium
        });
        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("Изменить радиус безумного фона на 30"),
            Category = VerbCategory.Debug,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
            Act = () => SetRangeSanity(component, 30f),
            Impact = LogImpact.Medium
        });

        if (component.IsStoper)
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("Выключить возможность остановить обелиск"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
                Act = () => SetStoper(component),
                Impact = LogImpact.Medium
            });
        }
        else
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("Включить возможность остановить обелиск"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
                Act = () => SetStoper(component),
                Impact = LogImpact.Medium
            });
        }


    }

    public void SetStoper(NecroobeliskComponent component)
    {
        component.IsStoper = !component.IsStoper;
    }

    public void SetDamageable(EntityUid uid, NecroobeliskComponent component)
    {
        if (HasComp<DamageableComponent>(uid))
        {
            RemComp<DamageableComponent>(uid);
        }
        else
        {
            AddComp<DamageableComponent>(uid);
        }
    }

    public void SetRangeSanity(NecroobeliskComponent component, float radius)
    {
        component.RangeSanity = radius;
    }

    public void ToggleObeliskActive(EntityUid target, NecroobeliskComponent component)
    {
        component.IsActive = !component.IsActive;
        UpdateState(target, component);
    }
}
