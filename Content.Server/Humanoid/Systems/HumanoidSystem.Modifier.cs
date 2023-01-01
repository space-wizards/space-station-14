using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Humanoid;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Server.Player;

namespace Content.Server.Humanoid;

public sealed partial class HumanoidSystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    private void OnVerbsRequest(EntityUid uid, HumanoidComponent component, GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
        {
            return;
        }

        if (!_adminManager.HasAdminFlag(actor.PlayerSession, AdminFlags.Fun))
        {
            return;
        }

        args.Verbs.Add(new Verb
        {
            Text = "Modify markings",
            Category = VerbCategory.Tricks,
            IconTexture = "/Textures/Mobs/Customization/reptilian_parts.rsi/tail_smooth.png",
            Act = () =>
            {
                _uiSystem.TryOpen(uid, HumanoidMarkingModifierKey.Key, actor.PlayerSession);
                _uiSystem.TrySetUiState(
                    uid,
                    HumanoidMarkingModifierKey.Key,
                    new HumanoidMarkingModifierState(component.CurrentMarkings, component.Species, component.SkinColor, component.CustomBaseLayers));
            }
        });
    }

    private void OnBaseLayersSet(EntityUid uid, HumanoidComponent component,
        HumanoidMarkingModifierBaseLayersSetMessage message)
    {
        if (message.Session is not IPlayerSession player
            || !_adminManager.HasAdminFlag(player, AdminFlags.Fun))
        {
            return;
        }

        if (message.Info == null)
        {
            component.CustomBaseLayers.Remove(message.Layer);
        }
        else
        {
            if (!component.CustomBaseLayers.TryAdd(message.Layer, message.Info))
            {
                component.CustomBaseLayers[message.Layer] = message.Info;
            }
        }

        Synchronize(uid, component);

        if (message.ResendState)
        {
            _uiSystem.TrySetUiState(
                uid,
                HumanoidMarkingModifierKey.Key,
                new HumanoidMarkingModifierState(component.CurrentMarkings, component.Species, component.SkinColor, component.CustomBaseLayers));
        }
    }

    private void OnMarkingsSet(EntityUid uid, HumanoidComponent component,
        HumanoidMarkingModifierMarkingSetMessage message)
    {
        if (message.Session is not IPlayerSession player
            || !_adminManager.HasAdminFlag(player, AdminFlags.Fun))
        {
            return;
        }

        component.CurrentMarkings = message.MarkingSet;
        Synchronize(uid, component);

        if (message.ResendState)
        {
            _uiSystem.TrySetUiState(
                uid,
                HumanoidMarkingModifierKey.Key,
                new HumanoidMarkingModifierState(component.CurrentMarkings, component.Species, component.SkinColor, component.CustomBaseLayers));
        }

    }
}
