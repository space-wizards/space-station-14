using Content.Server.Radiation.Components;
using Content.Server.Radiation.Events;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Shared.GameStates;

namespace Content.Server.Radiation.Systems;

public sealed class GeigerSystem : SharedGeigerSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeigerComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<OnRadiationSystemUpdated>(OnUpdate);
    }

    private void OnUpdate(OnRadiationSystemUpdated ev)
    {
        var query = EntityQuery<GeigerComponent, RadiationReceiverComponent>();
        foreach (var (geiger, receiver) in query)
        {
            var rads = receiver.CurrentRadiation;
            SetCurrentRadiation(geiger.Owner, geiger, rads);
        }
    }

    private void SetCurrentRadiation(EntityUid uid, SharedGeigerComponent component, float rads)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (rads == component.CurrentRadiation)
            return;

        component.CurrentRadiation = rads;
        component.DangerLevel = RadsToLevel(rads);
        UpdateAppearance(uid, component);
        Dirty(component);
    }

    private void UpdateAppearance(EntityUid uid, SharedGeigerComponent? component = null,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref component, ref appearance, false))
            return;

        _appearance.SetData(uid, GeigerVisuals.DangerLevel, component.DangerLevel, appearance);
    }

    private void OnGetState(EntityUid uid, GeigerComponent component, ref ComponentGetState args)
    {
        args.State = new GeigerComponentState
        {
            CurrentRadiation = component.CurrentRadiation,
            DangerLevel = component.DangerLevel
        };
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


