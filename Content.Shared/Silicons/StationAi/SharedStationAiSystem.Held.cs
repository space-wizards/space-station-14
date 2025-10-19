using Content.Shared.Actions.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    /*
     * Added when an entity is inserted into a StationAiCore.
     */

    //TODO: Fix this, please
    private const string JobNameLocId = "job-name-station-ai";

    private void InitializeHeld()
    {
        SubscribeLocalEvent<StationAiRadialMessage>(OnRadialMessage);
        SubscribeLocalEvent<StationAiWhitelistComponent, BoundUserInterfaceMessageAttempt>(OnMessageAttempt);
        SubscribeLocalEvent<StationAiWhitelistComponent, GetVerbsEvent<AlternativeVerb>>(OnTargetVerbs);

        SubscribeLocalEvent<StationAiHeldComponent, InteractionAttemptEvent>(OnHeldInteraction);
        SubscribeLocalEvent<StationAiHeldComponent, AttemptRelayActionComponentChangeEvent>(OnHeldRelay);
        SubscribeLocalEvent<StationAiHeldComponent, JumpToCoreEvent>(OnCoreJump);

        SubscribeLocalEvent<TryGetIdentityShortInfoEvent>(OnTryGetIdentityShortInfo);
    }

    private void OnTryGetIdentityShortInfo(TryGetIdentityShortInfoEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        if (!HasComp<StationAiHeldComponent>(args.ForActor))
        {
            return;
        }
        args.Title = $"{Name(args.ForActor)} ({Loc.GetString(JobNameLocId)})";
        args.Handled = true;
    }

    private void OnCoreJump(Entity<StationAiHeldComponent> ent, ref JumpToCoreEvent args)
    {
        if (!TryGetCore(ent.Owner, out var core) || core.Comp?.RemoteEntity == null)
            return;

        _xforms.DropNextTo(core.Comp.RemoteEntity.Value, core.Owner);
    }

    /// <summary>
    /// Tries to find an AI being held in by an entity using <see cref="StationAiHolderComponent"/>.
    /// </summary>
    /// <param name="entity">The station AI holder.</param>
    /// <param name="held">The found AI.</param>
    /// <returns>True if an AI is found.</returns>
    public bool TryGetHeld(Entity<StationAiHolderComponent?> entity, [NotNullWhen(true)] out EntityUid? held)
    {
        held = EntityUid.Invalid;

        if (!Resolve(entity.Owner, ref entity.Comp))
            return false;

        if (!_containers.TryGetContainer(entity.Owner, StationAiHolderComponent.Container, out var container) ||
            container.ContainedEntities.Count == 0)
            return false;

        held = container.ContainedEntities[0];
        return true;
    }


    /// <summary>
    /// Tries to find an AI being held in by an entity using <see cref="StationAiCoreComponent"/>.
    /// </summary>
    /// <param name="entity">The station AI core.</param>
    /// <param name="held">The found AI.</param>
    /// <returns>True if an AI is found.</returns>
    public bool TryGetHeld(Entity<StationAiCoreComponent?> entity, [NotNullWhen(true)] out EntityUid? held)
    {
        held = null;

        return TryComp<StationAiHolderComponent>(entity.Owner, out var holder) &&
            TryGetHeld((entity, holder), out held);
    }

    /// <summary>
    /// Tries to find the station AI core holding an AI.
    /// </summary>
    /// <param name="entity">The AI.</param>
    /// <param name="core">The found AI core.</param>
    /// <returns>True if an AI core is found.</returns>
    public bool TryGetCore(EntityUid entity, out Entity<StationAiCoreComponent?> core)
    {
        if (!_containers.TryGetContainingContainer(entity, out var container) ||
            container.ID != StationAiCoreComponent.Container ||
            !TryComp(container.Owner, out StationAiCoreComponent? coreComp))
        {
            core = (EntityUid.Invalid, null);
            return false;
        }

        core = (container.Owner, coreComp);
        return true;
    }

    private void OnHeldRelay(Entity<StationAiHeldComponent> ent, ref AttemptRelayActionComponentChangeEvent args)
    {
        if (!TryGetCore(ent.Owner, out var core))
            return;

        args.Target = core.Comp?.RemoteEntity;
    }

    private void OnRadialMessage(StationAiRadialMessage ev)
    {
        if (!TryGetEntity(ev.Entity, out var target))
            return;

        ev.Event.User = ev.Actor;
        RaiseLocalEvent(target.Value, (object) ev.Event);
    }

    private void OnMessageAttempt(Entity<StationAiWhitelistComponent> ent, ref BoundUserInterfaceMessageAttempt ev)
    {
        if (ev.Actor == ev.Target)
            return;

        if (TryComp(ev.Actor, out StationAiHeldComponent? aiComp) &&
           (!TryComp(ev.Target, out StationAiWhitelistComponent? whitelistComponent) ||
            !ValidateAi((ev.Actor, aiComp))))
        {
            // Don't allow the AI to interact with anything that isn't powered.
            if (!PowerReceiver.IsPowered(ev.Target))
            {
                ShowDeviceNotRespondingPopup(ev.Actor);
                ev.Cancel();
                return;
            }

            // Don't allow the AI to interact with anything that it isn't allowed to (ex. AI wire is cut)
            if (whitelistComponent is { Enabled: false })
            {
                ShowDeviceNotRespondingPopup(ev.Actor);
            }
            ev.Cancel();
        }
    }

    private void OnHeldInteraction(Entity<StationAiHeldComponent> ent, ref InteractionAttemptEvent args)
    {
        // Cancel if it's not us or something with a whitelist, or whitelist is disabled.
        args.Cancelled = (!TryComp(args.Target, out StationAiWhitelistComponent? whitelistComponent)
                          || !whitelistComponent.Enabled)
                         && ent.Owner != args.Target
                         && args.Target != null;
        if (whitelistComponent is { Enabled: false })
        {
            ShowDeviceNotRespondingPopup(ent.Owner);
        }
    }

    private void OnTargetVerbs(Entity<StationAiWhitelistComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!_uiSystem.HasUi(args.Target, AiUi.Key))
            return;

        if (!args.CanComplexInteract
            || !HasComp<StationAiHeldComponent>(args.User)
            || !args.CanInteract)
        {
            return;
        }

        var user = args.User;

        var target = args.Target;

        var isOpen = _uiSystem.IsUiOpen(target, AiUi.Key, user);

        var verb = new AlternativeVerb
        {
            Text = isOpen ? Loc.GetString("ai-close") : Loc.GetString("ai-open"),
            Act = () =>
            {
                if (isOpen)
                {
                    _uiSystem.CloseUi(ent.Owner, AiUi.Key, user);
                }
                else
                {
                    _uiSystem.OpenUi(ent.Owner, AiUi.Key, user);
                }
            }
        };
        args.Verbs.Add(verb);
    }

    private void ShowDeviceNotRespondingPopup(EntityUid toEntity)
    {
        _popup.PopupClient(Loc.GetString("ai-device-not-responding"), toEntity, PopupType.MediumCaution);
    }

    private void ShowDeviceNoAccessPopup(EntityUid toEntity)
    {
        _popup.PopupClient(Loc.GetString("ai-device-no-access"), toEntity, PopupType.MediumCaution);
    }
}

/// <summary>
/// Raised from client to server as a BUI message wrapping the event to perform.
/// Also handles AI action validation.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationAiRadialMessage : BoundUserInterfaceMessage
{
    public BaseStationAiAction Event = default!;
}

// Do nothing on server just here for shared move along.
/// <summary>
/// Raised on client to get the relevant data for radial actions.
/// </summary>
public sealed class StationAiRadial : BaseStationAiAction
{
    public SpriteSpecifier? Sprite;

    public string? Tooltip;

    public BaseStationAiAction Event = default!;
}

/// <summary>
/// Abstract parent for radial actions events.
/// When a client requests a radial action this will get sent.
/// </summary>
[Serializable, NetSerializable]
public abstract class BaseStationAiAction
{
    [field:NonSerialized]
    public EntityUid User { get; set; }
}

// No idea if there's a better way to do this.
/// <summary>
/// Grab actions possible for an AI on the target entity.
/// </summary>
[ByRefEvent]
public record struct GetStationAiRadialEvent()
{
    public List<StationAiRadial> Actions = new();
}

[Serializable, NetSerializable]
public enum AiUi : byte
{
    Key,
}
