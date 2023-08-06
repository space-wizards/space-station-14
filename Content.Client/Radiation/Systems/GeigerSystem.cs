using Content.Client.Items;
using Content.Client.Radiation.UI;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Client.Radiation.Systems;

public sealed class GeigerSystem : SharedGeigerSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeigerComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<GeigerComponent, ItemStatusCollectMessage>(OnGetStatusMessage);
    }

    private void OnHandleState(EntityUid uid, GeigerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not GeigerComponentState state)
            return;

        component.CurrentRadiation = state.CurrentRadiation;
        component.DangerLevel = state.DangerLevel;
        component.IsEnabled = state.IsEnabled;
        component.User = state.User;
        component.UiUpdateNeeded = true;
    }

    private void OnGetStatusMessage(EntityUid uid, GeigerComponent component, ItemStatusCollectMessage args)
    {
        if (!component.ShowControl)
            return;

        args.Controls.Add(new GeigerItemControl(component));
    }
}
