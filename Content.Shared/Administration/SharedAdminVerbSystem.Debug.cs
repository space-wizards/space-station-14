using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Configurable;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;

namespace Content.Shared.Administration;

public abstract partial class SharedAdminVerbSystem
{
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    protected virtual void AddDebugVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        // Delete verb
        if (_toolshedManager.ActivePermissionController?.CheckInvokable(new CommandSpec(_toolshedManager.DefaultEnvironment.GetCommand("delete"), null), player, out _) ?? false)
        {
            Verb deleteVerb = new()
            {
                Text = Loc.GetString("delete-verb-get-data-text"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png")),
                Act = () => Del(args.Target),
                Impact = LogImpact.Medium,
                ConfirmationPopup = true,
            };
            args.Verbs.Add(deleteVerb);
        }

        // Rejuvenate verb
        if (_toolshedManager.ActivePermissionController?.CheckInvokable(new CommandSpec(_toolshedManager.DefaultEnvironment.GetCommand("rejuvenate"), null), player, out _) ?? false)
        {
            Verb rejuvenateVerb = new()
            {
                Text = Loc.GetString("rejuvenate-verb-get-data-text"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png")),
                Act = () => DebugRejuvenateVerb(args.Target),
                Impact = LogImpact.Medium,
            };
            args.Verbs.Add(rejuvenateVerb);
        }

        // Control mob verb
        if (_toolshedManager.ActivePermissionController?.CheckInvokable(new CommandSpec(_toolshedManager.DefaultEnvironment.GetCommand("mind"), "control"), player, out _) ?? false)
        {
            Verb controlMobVerb = new()
            {
                Text = Loc.GetString("control-mob-verb-get-data-text"),
                Category = VerbCategory.Debug,
                // TODO VERB ICON control mob icon
                Act = () => _mindSystem.ControlMob(args.User, args.Target),
                Impact = LogImpact.High,
                ConfirmationPopup = true,
            };
            args.Verbs.Add(controlMobVerb);
        }

        // Make Sentient verb
        if (_adminManager.CanCommand(player, "makesentient") && args.User != args.Target && !HasComp<MindContainerComponent>(args.Target))
        {
            Verb makeSentientVerb = new()
            {
                Text = Loc.GetString("make-sentient-verb-get-data-text"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/sentient.svg.192dpi.png")),
                Act = () => _mindSystem.MakeSentient(args.Target),
                Impact = LogImpact.Medium,
            };
            args.Verbs.Add(makeSentientVerb);
        }

        if (TryComp<InventoryComponent>(args.Target, out var inventoryComponent))
        {
            // Strip all verb
            if (_adminManager.CanCommand(player, "stripall"))
            {
                Verb stripAllVerb = new()
                {
                    Text = Loc.GetString("strip-all-verb-get-data-text"),
                    Category = VerbCategory.Debug,
                    Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
                    Act = () => _consoleHost.RemoteExecuteCommand(player, $"stripall \"{args.Target}\""),
                    Impact = LogImpact.Medium,
                };
                args.Verbs.Add(stripAllVerb);
            }

            // set outfit verb
            if (_adminManager.CanCommand(player, "setoutfit"))
            {
                Verb setOutfitVerb = new()
                {
                    Text = Loc.GetString("set-outfit-verb-get-data-text"),
                    Category = VerbCategory.Debug,
                    Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
                    Act = () => DebugSetOutfitVerb(player, args.Target),
                    Impact = LogImpact.Medium,
                };
                args.Verbs.Add(setOutfitVerb);
            }
        }

        // In range unoccluded verb
        if (_adminManager.CanCommand(player, "inrangeunoccluded"))
        {
            Verb inRangeUnoccludedVerb = new()
            {
                Text = Loc.GetString("in-range-unoccluded-verb-get-data-text"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/information.svg.192dpi.png")),
                Act = () =>
                {

                    var message = _examineSystem.InRangeUnOccluded(args.User, args.Target)
                        ? Loc.GetString("in-range-unoccluded-verb-on-activate-not-occluded")
                        : Loc.GetString("in-range-unoccluded-verb-on-activate-occluded");

                    _popupSystem.PopupClient(message, args.Target, args.User);
                }
            };
            args.Verbs.Add(inRangeUnoccludedVerb);
        }

        // Make ghost role verb
        if (_adminManager.CanCommand(player, "makeghostrole") &&
            !(EntityManager.GetComponentOrNull<MindContainerComponent>(args.Target)?.HasMind ?? false))
        {
            Verb makeGhostRoleVerb = new()
            {
                Text = Loc.GetString("make-ghost-role-verb-get-data-text"),
                Category = VerbCategory.Debug,
                // TODO VERB ICON add ghost icon
                // Where is the national park service icon for haunted forests?
                Act = () => DebugMakeGhostRoleVerb(player, args.Target),
                Impact = LogImpact.Medium,
            };
            args.Verbs.Add(makeGhostRoleVerb);
        }

        // Need a suggestion for a better solution to this.
        var adminData = _adminManager.GetAdminData(player);

        if (adminData != null && adminData.CanAdminMenu() && TryComp(args.Target, out ConfigurationComponent? config))
        {
            Verb configureVerb = new()
            {
                Text = Loc.GetString("configure-verb-get-data-text"),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
                Category = VerbCategory.Debug,
                Act = () => _uiSystem.OpenUi(args.Target, ConfigurationComponent.ConfigurationUiKey.Key, actor.PlayerSession),
            };
            args.Verbs.Add(configureVerb);
        }

        // Add verb to open Solution Editor
        if (_adminManager.CanCommand(player, "addreagent") && HasComp<SolutionContainerManagerComponent>(args.Target))
        {
            Verb addReagentVerb = new()
            {
                Text = Loc.GetString("edit-solutions-verb-get-data-text"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/spill.svg.192dpi.png")),
                Act = () => DebugAddReagentVerb(player, args.Target),
                Impact = LogImpact.Medium, // maybe high depending on WHAT reagents they add...
            };
            args.Verbs.Add(addReagentVerb);
        }
    }

    protected virtual void DebugRejuvenateVerb(EntityUid target)
    {
    }

    protected virtual void DebugSetOutfitVerb(ICommonSession player, EntityUid target)
    {
    }

    protected virtual void DebugMakeGhostRoleVerb(ICommonSession player, EntityUid target)
    {
    }

    protected virtual void DebugAddReagentVerb(ICommonSession player, EntityUid target)
    {
    }
}
