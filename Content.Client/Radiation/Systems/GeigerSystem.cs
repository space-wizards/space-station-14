using Content.Client.Items;
using Content.Client.Radiation.Components;
using Content.Client.Radiation.UI;
using Content.Shared.Radiation.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Radiation.Systems;

public sealed class GeigerSystem : EntitySystem
{
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
        component.UiUpdateNeeded = true;
    }

    private void OnGetStatusMessage(EntityUid uid, GeigerComponent component, ItemStatusCollectMessage args)
    {
        args.Controls.Add(new GeigerItemControl(component));
    }

    public static GeigerDangerLevel RadsToLevel(float rads)
    {
        return rads switch
        {
            < 0.2f => GeigerDangerLevel.None,
            < 1f => GeigerDangerLevel.Low,
            < 3f => GeigerDangerLevel.Med,
            < 6f => GeigerDangerLevel.High,
            _ => GeigerDangerLevel.Extreme
        };
    }
}
