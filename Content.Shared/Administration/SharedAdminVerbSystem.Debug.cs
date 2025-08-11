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

    private void AddDebugVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        // Delete verb
        if (_toolshedManager.ActivePermissionController?.CheckInvokable(new CommandSpec(_toolshedManager.DefaultEnvironment.GetCommand("delete"), null), player, out _) ?? false)
        {
            Verb debugDeleteVerb = new()
            {
                Text = Loc.GetString("delete-verb-get-data-text"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png")),
                Act = () => Del(args.Target),
                Impact = LogImpact.Medium,
                ConfirmationPopup = true,
            };
            args.Verbs.Add(debugDeleteVerb);
        }

        // Rejuvenate verb
        if (_toolshedManager.ActivePermissionController?.CheckInvokable(new CommandSpec(_toolshedManager.DefaultEnvironment.GetCommand("rejuvenate"), null), player, out _) ?? false)
        {
            Verb debugRejuvenateVerb = new()
            {
                Text = Loc.GetString("rejuvenate-verb-get-data-text"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png")),
                Act = () => DebugRejuvenateVerb(args.Target),
                Impact = LogImpact.Medium,
            };
            args.Verbs.Add(debugRejuvenateVerb);
        }

        // Control mob verb
        if (_toolshedManager.ActivePermissionController?.CheckInvokable(new CommandSpec(_toolshedManager.DefaultEnvironment.GetCommand("mind"), "control"), player, out _) ?? false)
        {
            Verb debugControlMobVerb = new()
            {
                Text = Loc.GetString("control-mob-verb-get-data-text"),
                Category = VerbCategory.Debug,
                // TODO VERB ICON control mob icon
                Act = () => _mindSystem.ControlMob(args.User, args.Target),
                Impact = LogImpact.High,
                ConfirmationPopup = true,
            };
            args.Verbs.Add(debugControlMobVerb);
        }

        // Make Sentient verb
        if (CanCommandOverride(player, "makesentient") && args.User != args.Target && !HasComp<MindContainerComponent>(args.Target))
        {
            Verb debugMakeSentientVerb = new()
            {
                Text = Loc.GetString("make-sentient-verb-get-data-text"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/sentient.svg.192dpi.png")),
                Act = () => _mindSystem.MakeSentient(args.Target),
                Impact = LogImpact.Medium,
            };
            args.Verbs.Add(debugMakeSentientVerb);
        }

        if (TryComp<InventoryComponent>(args.Target, out var inventoryComponent))
        {
            // Strip all verb
            if (CanCommandOverride(player, "stripall"))
            {
                args.Verbs.Add(new Verb
                {
                    Text = Loc.GetString("strip-all-verb-get-data-text"),
                    Category = VerbCategory.Debug,
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
                    Act = () => _consoleHost.RemoteExecuteCommand(player, $"stripall \"{args.Target}\""),
                    Impact = LogImpact.Medium,
                });
            }

            // set outfit verb
            if (CanCommandOverride(player, "setoutfit"))
            {
                Verb verb = new()
                {
                    Text = Loc.GetString("set-outfit-verb-get-data-text"),
                    Category = VerbCategory.Debug,
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
                    Act = () => DebugSetOutfitVerb(player, args.Target),
                    Impact = LogImpact.Medium,
                };
                args.Verbs.Add(verb);
            }
        }

        // In range unoccluded verb
        if (CanCommandOverride(player, "inrangeunoccluded"))
        {
            Verb verb = new()
            {
                Text = Loc.GetString("in-range-unoccluded-verb-get-data-text"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/information.svg.192dpi.png")),
                Act = () =>
                {

                    var message = _examineSystem.InRangeUnOccluded(args.User, args.Target)
                        ? Loc.GetString("in-range-unoccluded-verb-on-activate-not-occluded")
                        : Loc.GetString("in-range-unoccluded-verb-on-activate-occluded");

                    _popupSystem.PopupClient(message, args.Target, args.User);
                }
            };
            args.Verbs.Add(verb);
        }

        // Make ghost role verb
        if (CanCommandOverride(player, "makeghostrole") &&
            !(EntityManager.GetComponentOrNull<MindContainerComponent>(args.Target)?.HasMind ?? false))
        {
            Verb verb = new()
            {
                Text = Loc.GetString("make-ghost-role-verb-get-data-text"),
                Category = VerbCategory.Debug,
                // TODO VERB ICON add ghost icon
                // Where is the national park service icon for haunted forests?
                Act = () => DebugMakeGhostRoleVerb(player, args.Target),
                Impact = LogImpact.Medium,
            };
            args.Verbs.Add(verb);
        }

        // Need a suggestion for a better solution to this.
        var adminData = _adminManager.GetAdminData(player);

        if (adminData != null && adminData.CanAdminMenu() && TryComp(args.Target, out ConfigurationComponent? config))
        {
            Verb verb = new()
            {
                Text = Loc.GetString("configure-verb-get-data-text"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
                Category = VerbCategory.Debug,
                Act = () => _uiSystem.OpenUi(args.Target,
                    ConfigurationComponent.ConfigurationUiKey.Key,
                    actor.PlayerSession),
            };
            args.Verbs.Add(verb);
        }

        // Add verb to open Solution Editor
        if (CanCommandOverride(player, "addreagent") && HasComp<SolutionContainerManagerComponent>(args.Target))
        {
            Verb verb = new()
            {
                Text = Loc.GetString("edit-solutions-verb-get-data-text"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/spill.svg.192dpi.png")),
                Act = () => DebugAddReagentVerb(player, args.Target),
                Impact = LogImpact.Medium, // maybe high depending on WHAT reagents they add...
            };
            args.Verbs.Add(verb);
        }
    }

    public virtual void DebugRejuvenateVerb(EntityUid target)
    {
    }

    public virtual void DebugSetOutfitVerb(ICommonSession player, EntityUid target)
    {
    }

    public virtual void DebugMakeGhostRoleVerb(ICommonSession player, EntityUid target)
    {
    }

    public virtual void DebugAddReagentVerb(ICommonSession player, EntityUid target)
    {
    }
}
