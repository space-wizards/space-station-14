// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Virus.Components;
using Content.Shared.Verbs;
using Content.Shared.Interaction.Components;
using Content.Shared.Hands.Components;
using Robust.Shared.Utility;
using Content.Shared.Database;
using Content.Shared.DeadSpace.TimeWindow;
using Robust.Shared.Random;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using System.Linq;
using Robust.Shared.Prototypes;
using Content.Shared.Destructible;
using Content.Shared.DeadSpace.Virus.Prototypes;
using Content.Shared.Body.Prototypes;
using Content.Shared.DeadSpace.Virus;

namespace Content.Server.DeadSpace.Virus.Systems;

public sealed class VirusMutationSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly VirusSystem _virus = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TimedWindowSystem _timedWindowSystem = default!;
    private ISawmill _sawmill = default!;

    /// <summary>
    ///     Зона поражения после разрушения сущности.
    /// </summary>
    private const float RangeInfectAfteDest = 10f;

    /// <summary>
    ///     Список всех body и симптомов, да, при загрузке прототипа body его тут не будет.
    /// </summary>
    private List<ProtoId<BodyPrototype>> _allBodyCache = new();
    private List<ProtoId<VirusSymptomPrototype>> _allSymptomsCache = new();


    /// <summary>
    ///     Сколько попыток за цикл симптомы будут мутировать.
    /// </summary>
    private const int MutateAttempts = 5;
    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("VirusMutationSystem");

        foreach (var proto in _prototype.EnumeratePrototypes<BodyPrototype>())
        {
            if (!BaseVirusSettings.BodyBlackList.Contains(proto.ID))
                _allBodyCache.Add(proto.ID);
        }

        foreach (var proto in _prototype.EnumeratePrototypes<VirusSymptomPrototype>())
            _allSymptomsCache.Add(proto.ID);

        SubscribeLocalEvent<VirusMutationComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VirusMutationComponent, GetVerbsEvent<Verb>>(DoSetVerbs);
        SubscribeLocalEvent<VirusMutationComponent, DestructionEventArgs>(OnDestr);
        SubscribeLocalEvent<VirusMutationComponent, CauseVirusEvent>(OnCauseVirus);
        SubscribeLocalEvent<VirusMutationComponent, CureVirusEvent>(OnCureVirus);
        SubscribeLocalEvent<VirusMutationComponent, ProbInfectAttemptEvent>(OnProbInfectAttempt);
    }

    private void OnInit(EntityUid uid, VirusMutationComponent component, ComponentInit args)
    {
        _timedWindowSystem.Reset(component.UpdateWindow);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VirusMutationComponent, VirusComponent>();
        while (query.MoveNext(out var uid, out var component, out var virus))
        {
            if (!_timedWindowSystem.IsExpired(component.UpdateWindow))
                continue;

            _timedWindowSystem.Reset(component.UpdateWindow);
            ProbMutate((uid, component, virus));
        }
    }

    private void OnProbInfectAttempt(EntityUid uid, VirusMutationComponent component, ProbInfectAttemptEvent args)
    {
        if (HasComp<VirusComponent>(uid))
            args.Cancel = true;
    }

    private void OnCauseVirus(Entity<VirusMutationComponent> entity, ref CauseVirusEvent args)
    {
        UpdateAppearance(entity, entity.Comp, true);
    }

    private void OnCureVirus(Entity<VirusMutationComponent> entity, ref CureVirusEvent args)
    {
        UpdateAppearance(entity, entity.Comp, false);
    }

    private void OnDestr(Entity<VirusMutationComponent> entity, ref DestructionEventArgs args)
    {
        if (!TryComp<VirusComponent>(entity, out var virus))
            return;

        _virus.InfectAround((entity, virus), RangeInfectAfteDest);
    }

    private void DoSetVerbs(EntityUid uid, VirusMutationComponent component, GetVerbsEvent<Verb> args)
    {
        if (!HasComp<ComplexInteractionComponent>(args.User) || !HasComp<HandsComponent>(args.User))
            return;

        if (!TryComp<VirusComponent>(uid, out var virus))
            return;

        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("virus-mutation-verb"),
            Category = VerbCategory.Debug,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
            Act = () =>
            {
                _virus.ProbInfect((uid, virus), args.User);
                _virus.CureVirus(uid, virus);
            },
            Impact = LogImpact.Medium
        });

    }

    public void ProbMutate(Entity<VirusMutationComponent?, VirusComponent?> host)
    {
        if (!Resolve(host, ref host.Comp1, false))
            return;

        if (!Resolve(host, ref host.Comp2, false))
            return;

        if (!CanMutate((host, host.Comp1, host.Comp2)))
            return;

        // Попытка мутации симптома
        MutateSymptom((host, host.Comp1, host.Comp2));

        // Попытка мутации расы
        MutateBody((host, host.Comp1, host.Comp2));
    }

    private void MutateSymptom(Entity<VirusMutationComponent?, VirusComponent?> host)
    {
        if (!Resolve(host, ref host.Comp1, false))
            return;

        if (!Resolve(host, ref host.Comp2, false))
            return;

        bool needRefresh = false;

        for (int i = 0; i < MutateAttempts; i++)
        {
            var available = new List<ProtoId<VirusSymptomPrototype>>();
            foreach (var protoId in _allSymptomsCache)
            {
                if (_prototype.TryIndex(protoId, out var candidate) &&
                    VirusSystem.CanAddSymptom(
                        host.Comp2.Data.ActiveSymptom,
                        protoId,
                        candidate,
                        isTaipan: false))
                {
                    available.Add(protoId);
                }
            }

            if (available.Count == 0)
                break;

            int index = _random.Next(available.Count);

            if (!_prototype.TryIndex(available[index], out var proto))
                continue;

            var price = _virus.GetSymptomPrice(host.Comp2.Data, proto);
            if (host.Comp2.Data.MutationPoints < price)
                continue;

            host.Comp2.Data.ActiveSymptom.Add(available[index]);

            host.Comp2.Data.MutationPoints -= price;

            _sawmill.Debug(
                $"Попытка мутации #{i + 1}: добавлен новый симптом '{proto.SymptomType}' ({proto.Name}) " +
                $"ТекущиеСимптомы=[{string.Join(", ", host.Comp2.Data.ActiveSymptom)}]"
            );

            needRefresh = true;
        }

        if (needRefresh)
            _virus.RefreshSymptoms((host, host.Comp2));

    }

    private void MutateBody(Entity<VirusMutationComponent?, VirusComponent?> host)
    {
        if (!Resolve(host, ref host.Comp1, false))
            return;

        if (!Resolve(host, ref host.Comp2, false))
            return;

        var available = _allBodyCache
            .Where(s => !host.Comp2.Data.BodyWhitelist.Contains(s))
            .ToList();

        if (available.Count == 0)
            return;

        var price = _virus.GetBodyPrice(host.Comp2.Data);
        if (host.Comp2.Data.MutationPoints < price)
            return;

        var pick = _random.Pick(available);
        host.Comp2.Data.BodyWhitelist.Add(pick);

        host.Comp2.Data.MutationPoints -= price;

        _sawmill.Debug(
            $"Добавлена новая раса: '{pick}'. " +
            $"Штамм='{host.Comp2.Data.StrainId}'. " +
            $"ТекущийWhitelist=[{string.Join(", ", host.Comp2.Data.BodyWhitelist)}]"
        );
    }

    public bool CanMutate(Entity<VirusMutationComponent?, VirusComponent?> host)
    {
        if (!Resolve(host, ref host.Comp1, false))
            return false;

        if (!Resolve(host, ref host.Comp2, false))
            return false;

        if (!HasComp<VirusComponent>(host))
            return false;

        // Если есть состояния, значит мутирует только живой
        if (TryComp<MobStateComponent>(host, out var mobState))
            return !_mobState.IsDead(host, mobState);

        return true;
    }

    private void UpdateAppearance(EntityUid uid, VirusMutationComponent component, bool isInfected)
    {
        if (!component.ChangeApperance)
            return;

        if (isInfected)
        {
            _appearance.SetData(uid, VirusMutationVisuals.state, false);
            _appearance.SetData(uid, VirusMutationVisuals.infected, true);
        }
        else
        {
            _appearance.SetData(uid, VirusMutationVisuals.state, true);
            _appearance.SetData(uid, VirusMutationVisuals.infected, false);
        }
    }
}
