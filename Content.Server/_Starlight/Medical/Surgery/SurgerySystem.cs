using System.Linq;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Humanoid;
using Content.Server.Popups;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Damage;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.HealthExaminable;
using Content.Shared.Interaction;
using Content.Shared.Prototypes;
using Content.Shared.Tag;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Starlight.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearanceSystem = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    private readonly List<EntProtoId> _surgeries = [];
    public override void Initialize()
    {
        base.Initialize();
        InitializeSteps();

        SubscribeLocalEvent<SurgeryToolComponent, AfterInteractEvent>(OnToolAfterInteract);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        LoadPrototypes();
    }

    public override void Update(float frameTime)
    {
        _delayAccumulator += frameTime;
        if (_delayAccumulator > 0.7)
        {
            _delayAccumulator = 0;
            while (_delayQueue.TryDequeue(out var action))
                action();
        }
    }

    protected override void RefreshUI(EntityUid body)
    {
        if (!HasComp<SurgeryTargetComponent>(body))
            return;

        var surgeries = new Dictionary<NetEntity, List<(EntProtoId, string suffix, bool isCompleted)>>();
        foreach (var part in _body.GetBodyChildren(body))
        {
            if (!TryComp<SurgeryProgressComponent>(part.Id, out var progress))
            {
                progress = new SurgeryProgressComponent();
                AddComp(part.Id, progress);
            }

            foreach (var surgery in _surgeries)
            {
                if (GetSingleton(surgery) is not { } surgeryEnt
                    || !TryComp(surgeryEnt, out SurgeryComponent? surgeryComp)
                    || (surgeryComp.Requirement.Count() > 0 && !progress.CompletedSurgeries.Any(x => surgeryComp.Requirement.Contains(x))))
                    continue;

                var ev = new SurgeryValidEvent(body, part.Id);

                var isCompleted = progress.CompletedSurgeries.Contains(surgery);
                if (!progress.StartedSurgeries.Contains(surgery) 
                    && !isCompleted)
                {
                    RaiseLocalEvent(surgeryEnt, ref ev);

                    if (ev.Cancelled)
                        continue;
                }

                surgeries.GetOrNew(GetNetEntity(part.Id)).Add((surgery, ev.Suffix, isCompleted));
            }
        }

        _ui.SetUiState(body, SurgeryUIKey.Key, new SurgeryBuiState() { Choices = surgeries });
    }

    private void OnToolAfterInteract(Entity<SurgeryToolComponent> ent, ref AfterInteractEvent args)
    {
        var user = args.User;
        if (args.Handled ||
            !args.CanReach ||
            args.Target == null ||
            _ui.IsUiOpen(user, SurgeryUIKey.Key, user) ||
            !HasComp<SurgeryTargetComponent>(args.Target)) return;

        if (user == args.Target)
        {
            _popup.PopupEntity("You can't perform surgery on yourself!", user, user);
            return;
        }

        args.Handled = true;
        _ui.OpenUi(args.Target.Value, SurgeryUIKey.Key, user);

        RefreshUI(args.Target.Value);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        _surgeries.Clear();

        foreach (var entity in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (entity.HasComponent<SurgeryComponent>())
                _surgeries.Add(new EntProtoId(entity.ID));
        }
    }
}
