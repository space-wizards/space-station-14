using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.CombatMode;
using Content.Shared.Popups;
using Content.Shared.Silicons.Bots;
using Content.Shared.Silicons.Bots.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.Silicons.Bots;

public sealed partial class SecuritronSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SecuritronComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SecuritronComponent, HTNComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out var securitron, out var htn, out var appearance))
        {
            htn.Blackboard.SetValue(NPCBlackboard.SecuritronOperatingModeKey, securitron.OperatingMode);

            var state = SecuritronVisualState.Online;

            if (TryComp<CombatModeComponent>(uid, out var combat) && combat.IsInCombatMode || HasComp<NPCMeleeCombatComponent>(uid))
                state = SecuritronVisualState.Combat;

            if (state == securitron.CurrentState)
                continue;

            securitron.CurrentState = state;

            _appearance.SetData(uid, SecuritronVisuals.State, state, appearance);
        }
    }

    private void OnGetAlternativeVerbs(EntityUid uid, SecuritronComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var nextMode = component.OperatingMode == SecuritronOperatingMode.Arrest
            ? SecuritronOperatingMode.Detain
            : SecuritronOperatingMode.Arrest;

        var nextModeName = Loc.GetString(GetModeLocale(nextMode));

        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("securitron-verb-set-mode", ("mode", nextModeName)),
            Icon = new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () =>
            {
                component.OperatingMode = nextMode;
                Dirty(uid, component);

                var modeName = Loc.GetString(GetModeLocale(component.OperatingMode));
                _popup.PopupEntity(Loc.GetString("securitron-popup-mode-changed", ("mode", modeName)), uid, args.User);
            }
        };

        args.Verbs.Add(verb);
    }

    private static string GetModeLocale(SecuritronOperatingMode mode)
    {
        return mode switch
        {
            SecuritronOperatingMode.Arrest => "securitron-mode-name-arrest",
            SecuritronOperatingMode.Detain => "securitron-mode-name-detain",
            _ => "securitron-mode-name-arrest",
        };
    }
}
