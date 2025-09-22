using System.Linq;
using Content.Server.Administration.UI;
using Content.Server.Disposal.Tube;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.GameTicking;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    protected override void AddDebugVerbs(GetVerbsEvent<Verb> args)
    {
        base.AddDebugVerbs(args);

        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        // TODO: This is a temporary solution. I will sort this soon. ~ Verin
        // Get Disposal tube direction verb
        if (!_adminManager.CanCommand(player, "tubeconnections") || !TryComp(args.Target, out DisposalTubeComponent? tube))
            return;

        Verb verb = new()
        {
            Text = Loc.GetString("tube-direction-verb-get-data-text"),
            Category = VerbCategory.Debug,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/information.svg.192dpi.png")),
            Act = () => _disposalTubes.PopupDirections(args.Target, tube, args.User),
        };
        args.Verbs.Add(verb);
    }

    protected override void DebugRejuvenateVerb(EntityUid target)
    {
        _rejuvenate.PerformRejuvenate(target);
    }

    protected override void DebugSetOutfitVerb(ICommonSession player, EntityUid target)
    {
        _euiManager.OpenEui(new SetOutfitEui(GetNetEntity(target)), player);
    }

    protected override void DebugMakeGhostRoleVerb(ICommonSession player, EntityUid target)
    {
        _ghostRoleSystem.OpenMakeGhostRoleEui(player, target);
    }

    protected override void DebugAddReagentVerb(ICommonSession player, EntityUid target)
    {
        OpenEditSolutionsEui(player, target);
    }

    #region SolutionsEui
    private void OnSolutionChanged(Entity<SolutionContainerManagerComponent> entity, ref SolutionContainerChangedEvent args)
    {
        foreach (var list in _openSolutionUis.Values)
        {
            foreach (var eui in list)
            {
                if (eui.Target == entity.Owner)
                    eui.StateDirty();
            }
        }
    }

    private void OpenEditSolutionsEui(ICommonSession session, EntityUid uid)
    {
        if (session.AttachedEntity == null)
            return;

        var eui = new EditSolutionsEui(uid);
        _euiManager.OpenEui(eui, session);
        eui.StateDirty();

        if (!_openSolutionUis.ContainsKey(session)) {
            _openSolutionUis[session] = new List<EditSolutionsEui>();
        }

        _openSolutionUis[session].Add(eui);
    }

    public void OnEditSolutionsEuiClosed(ICommonSession session, EditSolutionsEui eui)
    {
        _openSolutionUis[session].Remove(eui);
        if (_openSolutionUis[session].Count == 0)
            _openSolutionUis.Remove(session);
    }

    private void Reset(RoundRestartCleanupEvent ev)
    {
        foreach (var euis in _openSolutionUis.Values)
        {
            foreach (var eui in euis.ToList())
            {
                eui.Close();
            }
        }
        _openSolutionUis.Clear();
    }
    #endregion
}
