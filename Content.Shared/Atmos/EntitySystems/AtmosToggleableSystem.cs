using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Utility;

// Could rework this into not being atmos exclusive...
namespace Content.Shared.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class AtmosToggleableSystem : EntitySystem
    {

        private static AtmosToggleableEnabledEvent _enabledEv = new();
        private static AtmosToggleableDisabledEvent _disabledEv = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AtmosToggleableComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleVerb);
        }

        private void AddToggleVerb(EntityUid uid, AtmosToggleableComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!component.AltVerbAvailable)
                return;

            if (!args.CanInteract || !args.CanAccess)
                return;

            if (!HasComp<HandsComponent>(args.User))
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    if (!component.Enabled)
                        RaiseLocalEvent(uid, ref _enabledEv);
                    else
                        RaiseLocalEvent(uid, ref _disabledEv);
                },
                Priority = 1,
                Icon = component.Icon,
                Text = Loc.GetString(component.Text),
            };
            args.Verbs.Add(verb);
        }
    }
}

/// <summary>
///     Raised directed on an toggleable atmos entity when it is enabled.
/// </summary>
/// <remarks>
///     Should ideally only be handled by not more than one component on an entity.
/// </remarks>
[ByRefEvent]
public readonly record struct AtmosToggleableEnabledEvent;

/// <summary>
///     Raised directed on an toggleable atmos entity when it is disabled.
/// </summary>
/// <remarks>
///     Should ideally only be handled by not more than one component on an entity.
/// </remarks>
[ByRefEvent]
public readonly record struct AtmosToggleableDisabledEvent;
