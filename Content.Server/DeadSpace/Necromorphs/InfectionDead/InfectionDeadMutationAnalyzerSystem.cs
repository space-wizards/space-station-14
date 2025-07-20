// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Interaction;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead;
using Content.Shared.Paper;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;

namespace Content.Server.DeadSpace.Necromorphs.InfectionDead;

public sealed class InfectionDeadMutationAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InfectionDeadMutationAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    public override void Update(float frameTime)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<InfectionDeadMutationAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (curTime >= component.RunningTime && component.IsRunning)
                Running(uid, component);
        }
    }

    private void OnAfterInteract(EntityUid uid, InfectionDeadMutationAnalyzerComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null || args.Handled || component.IsRunning)
            return;

        if (!TryComp<NecromorfComponent>(args.Target, out var necro))
            return;

        component.User = args.User;
        component.IsRunning = true;
        component.StrainData = necro.StrainData;
        component.RunningTime = _gameTiming.CurTime + component.DurationRunning;
        _audio.PlayPvs(component.PrintingSound, uid);
        UpdateState(uid);
    }

    public void Running(EntityUid uid, InfectionDeadMutationAnalyzerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var strainData = component.StrainData;

        // Собираем свойства для вывода
        string output = "Параметры некроинфекции:\n";

        output += $"Модификатор урона: {strainData.DamageMulty}\n";
        output += $"Модификатор выносливости: {strainData.StaminaMulty}\n";
        output += $"Модификатор здоровья: {strainData.HpMulty}\n";
        output += $"Модификатор скорости: {strainData.SpeedMulty}\n";

        output += $"\nОсобые мутации: \n";

        output += strainData.Effects.ToString();

        var paper = Spawn(component.Paper, Transform(uid).Coordinates);

        if (!TryComp<PaperComponent>(paper, out var paperComp))
        {
            QueueDel(paper);
            return;
        }

        var content = Loc.GetString(output);

        _paperSystem.SetContent((paper, paperComp), content);

        component.IsRunning = false;
        UpdateState(uid);
    }

    public void UpdateState(EntityUid uid, InfectionDeadMutationAnalyzerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.IsRunning)
        {
            _appearance.SetData(uid, InfectionDeadMutationAnalyzerVisuals.Icon, false);
            _appearance.SetData(uid, InfectionDeadMutationAnalyzerVisuals.Working, true);
        }
        else
        {
            _appearance.SetData(uid, InfectionDeadMutationAnalyzerVisuals.Icon, true);
            _appearance.SetData(uid, InfectionDeadMutationAnalyzerVisuals.Working, false);
        }
    }
}

