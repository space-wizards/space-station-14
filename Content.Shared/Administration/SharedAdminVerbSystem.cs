using Content.Shared.Administration.Managers;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Movement.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Verbs;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;

namespace Content.Shared.Administration;

public abstract partial class SharedAdminVerbSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _adminManager = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ToolshedManager _toolshedManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(GetVerbs);
    }

    protected virtual void GetVerbs(GetVerbsEvent<Verb> args)
    {
        AddAdminVerbs(args);
        AddAntagVerbs(args);
        AddDebugVerbs(args);
        AddTricksVerbs(args);
    }

    private void AddAdminVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.IsAdmin(player))
            return;

        Verb markVerb = new()
        {
            Text = Loc.GetString("toolshed-verb-mark"),
            Message = Loc.GetString("toolshed-verb-mark-description"),
            Category = VerbCategory.Admin,
            Act = () => _toolshedManager.InvokeCommand(player, "=> $marked", new List<EntityUid> {args.Target}, out _),
            Impact = LogImpact.Low,
        };
        args.Verbs.Add(markVerb);

        if (TryComp(args.Target, out ActorComponent? targetActor))
        {
            // AdminHelp
            Verb ahelpVerb = new()
            {
                Text = Loc.GetString("ahelp-verb-get-data-text"),
                Category = VerbCategory.Admin,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/gavel.svg.192dpi.png")),
                Act = () => _consoleHost.RemoteExecuteCommand(player, $"openahelp \"{targetActor.PlayerSession.UserId}\""),
                Impact = LogImpact.Low,
            };
            args.Verbs.Add(ahelpVerb);

            // Subtle Messages
            Verb prayerVerb = new()
            {
                Text = Loc.GetString("prayer-verbs-subtle-message"),
                Category = VerbCategory.Admin,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/pray.svg.png")),
                Act = () => AdminPrayerVerb(player, targetActor.PlayerSession),
                Impact = LogImpact.Low,
            };
            args.Verbs.Add(prayerVerb);

            // Spawn - Like respawn but on the spot.
            Verb spawnVerb = new()
            {
                Text = Loc.GetString("admin-player-actions-spawn"),
                Category = VerbCategory.Admin,
                Act = () => AdminPlayerActionsSpawnVerb(args.User, args.Target, targetActor.PlayerSession),
                ConfirmationPopup = true,
                Impact = LogImpact.High,
            };
            args.Verbs.Add(spawnVerb);

            // Clone - Spawn but without the mind transfer, also spawns at the user's coordinates not the target's
            Verb cloneVerb = new()
            {
                Text = Loc.GetString("admin-player-actions-clone"),
                Category = VerbCategory.Admin,
                Act = () => AdminPlayerActionsCloneVerb(args.User, args.Target, targetActor.PlayerSession),
                ConfirmationPopup = true,
                Impact = LogImpact.High,
            };
            args.Verbs.Add(cloneVerb);

            // PlayerPanel
            Verb playerPanelVerb = new()
            {
                Text = Loc.GetString("admin-player-actions-player-panel"),
                Category = VerbCategory.Admin,
                Act = () => _consoleHost.ExecuteCommand(player, $"playerpanel \"{targetActor.PlayerSession.UserId}\""),
                Impact = LogImpact.Low,
            };
            args.Verbs.Add(playerPanelVerb);
        }

        if (_mindSystem.TryGetMind(args.Target, out var mindId, out var mindComp) && mindComp.UserId != null)
        {
            // Erase
            Verb eraseVerb = new()
            {
                Text = Loc.GetString("admin-verbs-erase"),
                Message = Loc.GetString("admin-verbs-erase-description"),
                Category = VerbCategory.Admin,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png")),
                Act = () => AdminEraseVerb(mindComp.UserId.Value),
                Impact = LogImpact.Extreme,
                ConfirmationPopup = true,
            };
            args.Verbs.Add(eraseVerb);

            // Respawn
            Verb respawnVerb = new()
            {
                Text = Loc.GetString("admin-player-actions-respawn"),
                Category = VerbCategory.Admin,
                Act = () => _consoleHost.ExecuteCommand(player, $"respawn \"{mindComp.UserId}\""),
                ConfirmationPopup = true,
                // No logimpact as the command does it internally.
            };
            args.Verbs.Add(respawnVerb);

            // Inspect mind
            Verb inspectMindVerb = new()
            {
                Text = Loc.GetString("inspect-mind-verb-get-data-text"),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/sentient.svg.192dpi.png")),
                Category = VerbCategory.Debug,
                Act = () => _consoleHost.RemoteExecuteCommand(player, $"vv {GetNetEntity(mindId)}"),
            };
            args.Verbs.Add(inspectMindVerb);
        }

        // Freeze
        var frozen = TryComp<AdminFrozenComponent>(args.Target, out var frozenComp);
        var frozenAndMuted = frozenComp?.Muted ?? false;

        if (!frozen)
        {
            Verb freezeVerb = new()
            {
                Priority = -1, // This is just so it doesn't change position in the menu between freeze/unfreeze.
                Text = Loc.GetString("admin-verbs-freeze"),
                Category = VerbCategory.Admin,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/snow.svg.192dpi.png")),
                Act = () => EnsureComp<AdminFrozenComponent>(args.Target),
                Impact = LogImpact.Medium,
            };
            args.Verbs.Add(freezeVerb);
        }

        if (!frozenAndMuted)
        {
            // allow you to additionally mute someone when they are already frozen
            Verb freezeAndMuteVerb = new()
            {
                Priority = -1, // This is just so it doesn't change position in the menu between freeze/unfreeze.
                Text = Loc.GetString("admin-verbs-freeze-and-mute"),
                Category = VerbCategory.Admin,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/snow.svg.192dpi.png")),
                Act = () => AdminFreezeVerb(args.Target),
                Impact = LogImpact.Medium,
            };
            args.Verbs.Add(freezeAndMuteVerb);
        }

        if (frozen)
        {
            Verb unfreezeVerb = new()
            {
                Priority = -1, // This is just so it doesn't change position in the menu between freeze/unfreeze.
                Text = Loc.GetString("admin-verbs-unfreeze"),
                Category = VerbCategory.Admin,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/snow.svg.192dpi.png")),
                Act = () => RemComp<AdminFrozenComponent>(args.Target),
                Impact = LogImpact.Medium,
            };
            args.Verbs.Add(unfreezeVerb);
        }


        // Admin Logs
        if (_adminManager.HasAdminFlag(player, AdminFlags.Logs))
        {
            Verb entityLogsVerb = new()
            {
                Priority = -2,
                Text = Loc.GetString("admin-verbs-admin-logs-entity"),
                Category = VerbCategory.Admin,
                Act = () => AdminEntityLogsVerb(player, args.Target),
                Impact = LogImpact.Low,
            };
            args.Verbs.Add(entityLogsVerb);
        }

        // TeleportTo
        Verb tpToVerb = new()
        {
            Text = Loc.GetString("admin-verbs-teleport-to"),
            Category = VerbCategory.Admin,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/open.svg.192dpi.png")),
            Act = () => _consoleHost.ExecuteCommand(player, $"tpto {GetNetEntity(args.Target)}"),
            Impact = LogImpact.Low,
        };
        args.Verbs.Add(tpToVerb);

        // TeleportHere
        Verb tpHereVerb = new()
        {
            Text = Loc.GetString("admin-verbs-teleport-here"),
            Category = VerbCategory.Admin,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/close.svg.192dpi.png")),
            Act = () =>
            {
                if (HasComp<MapGridComponent>(args.Target))
                {
                    if (player.AttachedEntity == null)
                        return;

                    var mapPos = _transformSystem.GetMapCoordinates(player.AttachedEntity.Value);

                    if (TryComp(args.Target, out PhysicsComponent? targetPhysics))
                    {
                        var offset = targetPhysics.LocalCenter;
                        var rotation = _transformSystem.GetWorldRotation(args.Target);

                        offset = rotation.RotateVec(offset);
                        mapPos = mapPos.Offset(-offset);
                    }

                    _consoleHost.ExecuteCommand(player, $"tpgrid {GetNetEntity(args.Target)} {mapPos.X} {mapPos.Y} {mapPos.MapId}");
                }
                else
                    _consoleHost.ExecuteCommand(player, $"tpto {args.User} {args.Target}");
            },
            Impact = LogImpact.Low,
        };
        args.Verbs.Add(tpHereVerb);

        // This logic is needed to be able to modify the AI's laws through its core and eye.
        EntityUid? target = null;

        if (TryComp(args.Target, out SiliconLawBoundComponent? lawBoundComponent))
            target = args.Target;

        // When inspecting the core we can find the entity with its laws by looking at the  AiHolderComponent.
        else if (TryComp<StationAiHolderComponent>(args.Target, out var holder) && holder.Slot.Item != null && TryComp(holder.Slot.Item, out lawBoundComponent))
        {
            target = holder.Slot.Item.Value;
            // For the eye we can find the entity with its laws as the source of the movement relay since the eye
            // is just a proxy for it to move around and look around the station.
        }

        else if (TryComp<MovementRelayTargetComponent>(args.Target, out var relay) && TryComp(relay.Source, out lawBoundComponent))
            target = relay.Source;

        if (lawBoundComponent != null && target != null && _adminManager.HasAdminFlag(player, AdminFlags.Moderator))
        {
            Verb siliconLawVerb = new()
            {
                Text = Loc.GetString("silicon-law-ui-verb"),
                Category = VerbCategory.Admin,
                Act = () => AdminSiliconLawsVerb(player, args.Target, lawBoundComponent),
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/actions_borg.rsi"), "state-laws"),
            };
            args.Verbs.Add(siliconLawVerb);
        }

        // open camera
        Verb cameraVerb = new()
        {
            Priority = 10,
            Text = Loc.GetString("admin-verbs-camera"),
            Message = Loc.GetString("admin-verbs-camera-description"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/vv.svg.192dpi.png")),
            Category = VerbCategory.Admin,
            Act = () => AdminCameraVerb(player, args.Target),
            Impact = LogImpact.Low,
        };
        args.Verbs.Add(cameraVerb);
    }


    // This is a solution to deal with the fact theres no shared way to check command perms.
    // Should the ConGroupControllers be unified and shared, this should be replaced with that instead.
    public virtual bool CanCommandOverride(ICommonSession player, string command)
    {
        return false;
    }

    public virtual void AdminPrayerVerb(ICommonSession player, ICommonSession target)
    {
    }

    public virtual void AdminPlayerActionsSpawnVerb(EntityUid user, EntityUid target, ICommonSession targetSession)
    {
    }

    public virtual void AdminPlayerActionsCloneVerb(EntityUid user, EntityUid target, ICommonSession targetSession)
    {
    }

    public virtual void AdminEraseVerb(NetUserId target)
    {
    }

    public virtual void AdminFreezeVerb(EntityUid target)
    {
    }

    public virtual void AdminEntityLogsVerb(ICommonSession player, EntityUid target)
    {
    }

    public virtual void AdminSiliconLawsVerb(ICommonSession player, EntityUid target, SiliconLawBoundComponent comp)
    {
    }

    public virtual void AdminCameraVerb(ICommonSession player, EntityUid target)
    {
    }
}
