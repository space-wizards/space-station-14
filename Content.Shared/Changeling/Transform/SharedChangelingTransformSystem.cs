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

public abstract partial class SharedChangelingTransformSystem : EntitySystem
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingTransformComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformActionEvent>(OnTransformAction);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformWindupDoAfterEvent>(OnSuccessfulTransform);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformIdentitySelectMessage>(OnTransformSelected);
    }

    private void OnMapInit(Entity<ChangelingTransformComponent> ent, ref MapInitEvent init)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.ChangelingTransformActionEntity, ent.Comp.ChangelingTransformAction);

        var userInterfaceComp = EnsureComp<UserInterfaceComponent>(ent);
        _uiSystem.SetUi((ent, userInterfaceComp), TransformUi.Key, new InterfaceData("ChangelingTransformBoundUserInterface"));
    }

    protected void OnTransformAction(Entity<ChangelingTransformComponent> ent,
        ref ChangelingTransformActionEvent args)
    {
        if (!HasComp<UserInterfaceComponent>(ent))
            return;

        if (!TryComp<ChangelingIdentityComponent>(ent, out var userIdentity))
            return;

        if (!_uiSystem.IsUiOpen(ent.Owner, TransformUi.Key, args.Performer))
        {
            _uiSystem.OpenUi(ent.Owner, TransformUi.Key, args.Performer);

            var identityData = new List<ChangelingIdentityData>();

            foreach (var consumedIdentity in userIdentity.ConsumedIdentities)
            {
                identityData.Add(new ChangelingIdentityData(GetNetEntity(consumedIdentity), Name(consumedIdentity)));
            }

            _uiSystem.SetUiState(ent.Owner, TransformUi.Key, new ChangelingTransformBoundUserInterfaceState(identityData));
        }
        else // if the UI is already opened and the command action is done again, transform into the last consumed identity
        {
            TransformPreviousConsumed(ent);
            _uiSystem.CloseUi(ent.Owner, TransformUi.Key, args.Performer);
        }
    }

    private void TransformPreviousConsumed(Entity<ChangelingTransformComponent> ent)
    {
        if (!TryComp<ChangelingIdentityComponent>(ent, out var identity))
            return;

        _popupSystem.PopupPredicted(Loc.GetString("changeling-transform-attempt"), ent, null, PopupType.MediumCaution);

        if(_net.IsServer) // Gotta do this on the server and with PlayPvs cause PlayPredicted doesn't return the Entity
            ent.Comp.CurrentTransformSound = _audio.PlayPvs(ent.Comp.TransformAttemptNoise, ent, new AudioParams())!.Value.Entity;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.TransformWindup,
            new ChangelingTransformWindupDoAfterEvent(GetNetEntity(identity.LastConsumedEntityUid!.Value)),
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
        _uiSystem.CloseUi(ent.Owner, TransformUi.Key, ent);

        var selectedIdentity = args.TargetIdentity;

        _popupSystem.PopupPredicted(Loc.GetString("changeling-transform-attempt"), ent, null, PopupType.MediumCaution);

        if(_net.IsServer)
           ent.Comp.CurrentTransformSound = _audio.PlayPvs(ent.Comp.TransformAttemptNoise, ent, new AudioParams())!.Value.Entity;

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} begun an attempt to transform into \"{Name(GetEntity(selectedIdentity))}\"");

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.TransformWindup,
            new ChangelingTransformWindupDoAfterEvent(selectedIdentity),
            ent,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            DuplicateCondition = DuplicateConditions.None,
        });
    }


    private void OnSuccessfulTransform(Entity<ChangelingTransformComponent> ent,
       ref ChangelingTransformWindupDoAfterEvent args)
    {
        args.Handled = true;

        _audio.Stop(ent.Comp.CurrentTransformSound);
        if (args.Cancelled)
            return;

        var targetIdentity = GetEntity(args.TargetIdentity);

        _humanoidAppearanceSystem.CloneAppearance(targetIdentity, args.User);
        ApplyComponentChanges(targetIdentity, ent, ent.Comp.TransformCloningSettings);


        //TODO: While it would be splendid to be able to provide the original owning player who was playing the targetIdentity, it's not exactly feasible to do
        _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(ent.Owner):player}  successfully transformed into \"{Name(targetIdentity)}\"");
        _metaSystem.SetEntityName(ent, Name(targetIdentity), raiseEvents: false);
        _metaSystem.SetEntityDescription(ent, MetaData(targetIdentity).EntityDescription);

        Dirty(ent);
    }

    protected virtual void ApplyComponentChanges(EntityUid ent, EntityUid target, ProtoId<CloningSettingsPrototype> settingsId) { }
}


