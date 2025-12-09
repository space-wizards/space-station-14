using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Shared.Silicons.StationAi.EntitySystems;

public abstract class SharedAiCameraJammerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AiCameraJammerComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<AiCameraJammerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnGetVerb(Entity<AiCameraJammerComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        byte index = 0;
        foreach (var setting in entity.Comp.Settings)
        {
            var currIndex = index;
            var verb = new Verb
            {
                Priority = currIndex,
                Category = VerbCategory.PowerLevel,
                Disabled = entity.Comp.SelectedPowerLevel == currIndex,
                Act = () =>
                {
                    var oldLevel = entity.Comp.SelectedPowerLevel;
                    entity.Comp.SelectedPowerLevel = currIndex;
                    Dirty(entity);
                    Popup.PopupClient(Loc.GetString(setting.Message), user, user);

                    // Raise event if power level actually changed
                    if (oldLevel != currIndex)
                    {
                        var ev = new AiCameraJammerPowerLevelChangedEvent(oldLevel, currIndex);
                        RaiseLocalEvent(entity, ref ev);
                    }
                },
                Text = Loc.GetString(setting.Name),
            };
            args.Verbs.Add(verb);
            index++;
        }
    }

    private void OnExamine(Entity<AiCameraJammerComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            var powerIndicator = HasComp<ActiveAiCameraJammerComponent>(ent)
                ? Loc.GetString("ai-camera-jammer-examine-on-state")
                : Loc.GetString("ai-camera-jammer-examine-off-state");
            args.PushMarkup(powerIndicator);

            var powerLevel = Loc.GetString(ent.Comp.Settings[ent.Comp.SelectedPowerLevel].Name);
            var switchIndicator = Loc.GetString("ai-camera-jammer-switch-setting", ("powerLevel", powerLevel));
            args.PushMarkup(switchIndicator);
        }
    }

    public float GetCurrentWattage(Entity<AiCameraJammerComponent> jammer)
    {
        return jammer.Comp.Settings[jammer.Comp.SelectedPowerLevel].Wattage;
    }

    public float GetCurrentRange(Entity<AiCameraJammerComponent> jammer)
    {
        return jammer.Comp.Settings[jammer.Comp.SelectedPowerLevel].Range;
    }

    protected void ChangeLEDState(Entity<AppearanceComponent?> ent, bool isLEDOn)
    {
        _appearance.SetData(ent, AiCameraJammerVisuals.LEDOn, isLEDOn, ent.Comp);
    }

    protected void ChangeChargeLevel(Entity<AppearanceComponent?> ent, AiCameraJammerChargeLevel chargeLevel)
    {
        _appearance.SetData(ent, AiCameraJammerVisuals.ChargeLevel, chargeLevel, ent.Comp);
    }
}
