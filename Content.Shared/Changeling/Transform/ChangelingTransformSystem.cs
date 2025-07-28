using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Cloning;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Transform;

public sealed partial class ChangelingTransformSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearanceSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCloningSystem _cloningSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private const string ChangelingBuiXmlGeneratedName = "ChangelingTransformBoundUserInterface";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingTransformComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformActionEvent>(OnTransformAction);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformDoAfterEvent>(OnSuccessfulTransform);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformIdentitySelectMessage>(OnTransformSelected);
        SubscribeLocalEvent<ChangelingTransformComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<ChangelingTransformComponent> ent, ref MapInitEvent init)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.ChangelingTransformActionEntity, ent.Comp.ChangelingTransformAction);

        var userInterfaceComp = EnsureComp<UserInterfaceComponent>(ent);
        _uiSystem.SetUi((ent, userInterfaceComp), TransformUI.Key, new InterfaceData(ChangelingBuiXmlGeneratedName));
    }

    private void OnShutdown(Entity<ChangelingTransformComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ChangelingTransformActionEntity != null)
        {
            _actionsSystem.RemoveAction(ent.Owner, ent.Comp.ChangelingTransformActionEntity);
        }
    }

    private void OnTransformAction(Entity<ChangelingTransformComponent> ent,
        ref ChangelingTransformActionEvent args)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var userInterfaceComp))
            return;

        if (!TryComp<ChangelingIdentityComponent>(ent, out var userIdentity))
            return;

        if (!_uiSystem.IsUiOpen((ent, userInterfaceComp), TransformUI.Key, args.Performer))
        {
            _uiSystem.OpenUi((ent, userInterfaceComp), TransformUI.Key, args.Performer);

            var identityData = new List<NetEntity>();

            foreach (var consumedIdentity in userIdentity.ConsumedIdentities)
            {
                identityData.Add(GetNetEntity(consumedIdentity));
            }

            _uiSystem.SetUiState((ent, userInterfaceComp), TransformUI.Key, new ChangelingTransformBoundUserInterfaceState(identityData));
        }
        else // if the UI is already opened and the command action is done again, transform into the last consumed identity
        {
            TransformPreviousConsumed(ent);
            _uiSystem.CloseUi((ent, userInterfaceComp), TransformUI.Key, args.Performer);
        }
    }

    private void TransformPreviousConsumed(Entity<ChangelingTransformComponent> ent)
    {
        if (!TryComp<ChangelingIdentityComponent>(ent, out var identity))
            return;

        _popupSystem.PopupPredicted(Loc.GetString("changeling-transform-attempt"),
            Loc.GetString("changeling-transform-attempt-others", ("user", ent)),
            ent,
            ent,
            PopupType.MediumCaution);

        if (_net.IsServer) // Gotta do this on the server and with PlayPvs cause PlayPredicted doesn't return the Entity
        {
            var pvsSound = _audio.PlayPvs(ent.Comp.TransformAttemptNoise, ent);
            if (pvsSound != null)
                ent.Comp.CurrentTransformSound = pvsSound.Value.Entity;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.TransformWindup,
            new ChangelingTransformDoAfterEvent(GetNetEntity(identity.LastConsumedEntityUid!.Value)),
            ent,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            DuplicateCondition = DuplicateConditions.None,
        });
    }

    private void OnTransformSelected(Entity<ChangelingTransformComponent> ent,
        ref ChangelingTransformIdentitySelectMessage args)
    {
        _uiSystem.CloseUi(ent.Owner, TransformUI.Key, ent);

        var selectedIdentity = args.TargetIdentity;

        _popupSystem.PopupPredicted(Loc.GetString("changeling-transform-attempt"),
            Loc.GetString("changeling-transform-attempt-others", ("user", ent)),
            ent,
            ent,
            PopupType.MediumCaution);

        if (_net.IsServer)
            ent.Comp.CurrentTransformSound = _audio.PlayPvs(ent.Comp.TransformAttemptNoise, ent, new AudioParams())!.Value.Entity;

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} begun an attempt to transform into \"{Name(GetEntity(selectedIdentity))}\"");

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.TransformWindup,
            new ChangelingTransformDoAfterEvent(selectedIdentity),
            ent,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            DuplicateCondition = DuplicateConditions.None,
        });
    }

    private void OnSuccessfulTransform(Entity<ChangelingTransformComponent> ent,
        ref ChangelingTransformDoAfterEvent args)
    {
        args.Handled = true;

        if (EntityManager.EntityExists(ent.Comp.CurrentTransformSound))
            _audio.Stop(ent.Comp.CurrentTransformSound);

        if (args.Cancelled)
            return;

        if (!_prototype.Resolve(ent.Comp.TransformCloningSettings, out var settings))
            return;

        var targetIdentity = GetEntity(args.TargetIdentity);

        _humanoidAppearanceSystem.CloneAppearance(targetIdentity, args.User);
        _cloningSystem.CloneComponents(targetIdentity, args.User, settings);

        //TODO: While it would be splendid to be able to provide the original owning player who was playing the targetIdentity, it's not exactly feasible to do
        _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(ent.Owner):player} successfully transformed into \"{Name(targetIdentity)}\"");
        _metaSystem.SetEntityName(ent, Name(targetIdentity), raiseEvents: false);

        Dirty(ent);
    }
}
