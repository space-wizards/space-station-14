using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Body;
using Content.Shared.Changeling.Components;
using Content.Shared.Cloning;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Systems;

public sealed partial class ChangelingTransformSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCloningSystem _cloningSystem = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
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
        _uiSystem.SetUi((ent, userInterfaceComp), ChangelingTransformUiKey.Key, new InterfaceData(ChangelingBuiXmlGeneratedName));
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

        if (!_uiSystem.IsUiOpen((ent, userInterfaceComp), ChangelingTransformUiKey.Key, args.Performer))
        {
            _uiSystem.OpenUi((ent, userInterfaceComp), ChangelingTransformUiKey.Key, args.Performer);
        } //TODO: Can add a Else here with TransformInto and CloseUI to make a quick switch,
          // issue right now is that Radials cover the Action buttons so clicking the action closes the UI (due to clicking off a radial causing it to close, even with UI)
          // but pressing the number does.
    }

    /// <summary>
    /// Transform the changeling into another identity.
    /// This can be any cloneable humanoid and doesn't have to be stored in the ChangelingIdentiyComponent,
    /// so make sure to validate the target before.
    /// </summary>
    public void TransformInto(Entity<ChangelingTransformComponent?> ent, EntityUid targetIdentity)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var selfMessage = Loc.GetString("changeling-transform-attempt-self", ("user", Identity.Entity(ent.Owner, EntityManager)));
        var othersMessage = Loc.GetString("changeling-transform-attempt-others", ("user", Identity.Entity(ent.Owner, EntityManager)));
        _popupSystem.PopupPredicted(
            selfMessage,
            othersMessage,
            ent,
            ent,
            PopupType.MediumCaution);

        if (_net.IsServer)
            ent.Comp.CurrentTransformSound = _audio.PlayPvs(ent.Comp.TransformAttemptNoise, ent)?.Entity;

        if (TryComp<ChangelingStoredIdentityComponent>(targetIdentity, out var storedIdentity) && storedIdentity.OriginalSession != null)
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} begun an attempt to transform into \"{Name(targetIdentity)}\" ({storedIdentity.OriginalSession:player}) ");
        else
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} begun an attempt to transform into \"{Name(targetIdentity)}\"");

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            ent,
            ent.Comp.TransformWindup,
            new ChangelingTransformDoAfterEvent(),
            ent,
            target: targetIdentity)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            DuplicateCondition = DuplicateConditions.None,
            RequireCanInteract = false,
            DistanceThreshold = null,
        });
    }

    private void OnTransformSelected(Entity<ChangelingTransformComponent> ent,
        ref ChangelingTransformIdentitySelectMessage args)
    {
        _uiSystem.CloseUi(ent.Owner, ChangelingTransformUiKey.Key, ent);

        if (!TryGetEntity(args.TargetIdentity, out var targetIdentity))
            return;

        if (!TryComp<ChangelingIdentityComponent>(ent, out var identity))
            return;

        if (identity.CurrentIdentity == targetIdentity)
            return; // don't transform into ourselves

        if (!identity.ConsumedIdentities.Contains(targetIdentity.Value))
            return; // this identity does not belong to this player

        TransformInto(ent.AsNullable(), targetIdentity.Value);
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

        if (args.Target is not { } targetIdentity)
            return;

        _visualBody.CopyAppearanceFrom(targetIdentity, args.User);
        _cloningSystem.CloneComponents(targetIdentity, args.User, settings);

        if (TryComp<ChangelingStoredIdentityComponent>(targetIdentity, out var storedIdentity) && storedIdentity.OriginalSession != null)
            _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(ent.Owner):player} successfully transformed into \"{Name(targetIdentity)}\" ({storedIdentity.OriginalSession:player})");
        else
            _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(ent.Owner):player} successfully transformed into \"{Name(targetIdentity)}\"");
        _metaSystem.SetEntityName(ent, Name(targetIdentity), raiseEvents: false);

        Dirty(ent);

        if (TryComp<ChangelingIdentityComponent>(ent, out var identity)) // in case we ever get changelings that don't store identities
        {
            identity.CurrentIdentity = targetIdentity;
            Dirty(ent.Owner, identity);
        }
    }
}
