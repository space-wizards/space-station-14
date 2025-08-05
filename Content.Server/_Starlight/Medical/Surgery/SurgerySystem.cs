using System.Linq;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Body.Part;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Prototypes;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Server.Administration.Systems;

namespace Content.Server.Starlight.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ContainerSystem _containers = default!;

    private readonly List<EntProtoId> _surgeries = [];
    public override void Initialize()
    {
        base.Initialize();
        InitializeSteps();

        SubscribeLocalEvent<SurgeryToolComponent, AfterInteractEvent>(OnToolAfterInteract);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        LoadPrototypes();
    }

    protected override void RefreshUI(EntityUid body)
    {
        if (!HasComp<SurgeryTargetComponent>(body))
            return;

        var surgeries = new Dictionary<NetEntity, List<(EntProtoId, string suffix, bool isCompleted)>>();
        if (HasComp<BodyPartComponent>(body))
        {
            AddSurgeries(body, body, surgeries);
        }
        else
        {
            foreach (var part in _body.GetBodyChildren(body))
            {
                AddSurgeries(part.Id, body, surgeries);
            }
        }

        _ui.SetUiState(body, SurgeryUIKey.Key, new SurgeryBuiState() { Choices = surgeries });
    }

    private void AddSurgeries(EntityUid part, EntityUid body, Dictionary<NetEntity, List<(EntProtoId, string suffix, bool isCompleted)>> surgeries)
    {
        if (!TryComp<SurgeryProgressComponent>(part, out var progress))
        {
            progress = new SurgeryProgressComponent();
            AddComp(part, progress);
        }

        foreach (var surgery in _surgeries)
        {
            if (!_entity.TryGetSingleton(surgery, out var surgeryEnt)
                || !TryComp(surgeryEnt, out SurgeryComponent? surgeryComp)
                || (surgeryComp.Requirement.Count() > 0 && !progress.CompletedSurgeries.Any(x => surgeryComp.Requirement.Contains(x))))
                continue;

            var ev = new SurgeryValidEvent(body, part);

            var isCompleted = progress.CompletedSurgeries.Contains(surgery);
            if (!progress.StartedSurgeries.Contains(surgery)
                && !isCompleted)
            {
                RaiseLocalEvent(surgeryEnt, ref ev);

                if (ev.Cancelled)
                    continue;
            }

            surgeries.GetOrNew(GetNetEntity(part)).Add((surgery, ev.Suffix, isCompleted));
        }
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
