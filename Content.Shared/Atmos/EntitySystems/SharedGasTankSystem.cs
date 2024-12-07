using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Examine;
using Content.Shared.Toggleable;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using InternalsComponent = Content.Shared.Body.Components.InternalsComponent;

namespace Content.Shared.Atmos.EntitySystems;

public abstract class SharedGasTankSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedInternalsSystem _internals = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasTankComponent, ComponentShutdown>(OnGasShutdown);
        SubscribeLocalEvent<GasTankComponent, BeforeActivatableUIOpenEvent>(BeforeUiOpen);
        SubscribeLocalEvent<GasTankComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<GasTankComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<GasTankComponent, ToggleActionEvent>(OnActionToggle);
        SubscribeLocalEvent<GasTankComponent, GasTankSetPressureMessage>(OnGasTankSetPressure);
        SubscribeLocalEvent<GasTankComponent, GasTankToggleInternalsMessage>(OnGasTankToggleInternals);
        SubscribeLocalEvent<GasTankComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerb);
    }

    private void OnGasShutdown(Entity<GasTankComponent> gasTank, ref ComponentShutdown args)
    {
        DisconnectFromInternals(gasTank);
    }

    private void OnGasTankToggleInternals(Entity<GasTankComponent> ent, ref GasTankToggleInternalsMessage args)
    {
        ToggleInternals(ent);
    }

    private void OnGasTankSetPressure(Entity<GasTankComponent> ent, ref GasTankSetPressureMessage args)
    {
        var pressure = Math.Clamp(args.Pressure, 0f, ent.Comp.MaxOutputPressure);

        ent.Comp.OutputPressure = pressure;

        UpdateUserInterface(ent, true);
    }

    public void UpdateUserInterface(Entity<GasTankComponent> ent, bool initialUpdate = false)
    {
        var (owner, component) = ent;
        _ui.SetUiState(owner, SharedGasTankUiKey.Key,
            new GasTankBoundUserInterfaceState
            {
                TankPressure = component.Air?.Pressure ?? 0,
                OutputPressure = initialUpdate ? component.OutputPressure : null,
                InternalsConnected = component.IsConnected,
                CanConnectInternals = CanConnectToInternals(ent)
            });
    }

    private void BeforeUiOpen(Entity<GasTankComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        // Only initial update includes output pressure information, to avoid overwriting client-input as the updates come in.
        UpdateUserInterface(ent, true);
    }

    private void OnGetActions(EntityUid uid, GasTankComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
    }

    private void OnExamined(EntityUid uid, GasTankComponent component, ExaminedEvent args)
    {
        using var _ = args.PushGroup(nameof(GasTankComponent));
        if (args.IsInDetailsRange)
            args.PushMarkup(Loc.GetString("comp-gas-tank-examine", ("pressure", Math.Round(component.Air?.Pressure ?? 0))));
        if (component.IsConnected)
            args.PushMarkup(Loc.GetString("comp-gas-tank-connected"));
        args.PushMarkup(Loc.GetString(component.IsValveOpen ? "comp-gas-tank-examine-open-valve" : "comp-gas-tank-examine-closed-valve"));
    }

    private void OnActionToggle(Entity<GasTankComponent> gasTank, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        ToggleInternals(gasTank);
        args.Handled = true;
    }

    private void OnGetAlternativeVerb(EntityUid uid, GasTankComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;
        args.Verbs.Add(new AlternativeVerb()
        {
            Text = component.IsValveOpen ? Loc.GetString("comp-gas-tank-close-valve") : Loc.GetString("comp-gas-tank-open-valve"),
            Act = () =>
            {
                component.IsValveOpen = !component.IsValveOpen;
                _audio.PlayPredicted(component.ValveSound, uid, args.User);
            },
            Disabled = component.IsConnected,
        });
    }

    public bool CanConnectToInternals(Entity<GasTankComponent> ent)
    {
        TryGetInternalsComp(ent, out _, out var internalsComp, ent.Comp.User);
        return internalsComp != null && internalsComp.BreathTools.Count != 0 && !ent.Comp.IsValveOpen;
    }

    public void ConnectToInternals(Entity<GasTankComponent> ent, EntityUid? user = null)
    {
        var (owner, component) = ent;
        if (component.IsConnected || !CanConnectToInternals(ent))
            return;

        TryGetInternalsComp(ent, out var internalsUid, out var internalsComp, ent.Comp.User);
        if (internalsUid == null || internalsComp == null)
            return;

        if (_internals.TryConnectTank((internalsUid.Value, internalsComp), owner))
            component.User = internalsUid.Value;

        _actions.SetToggled(component.ToggleActionEntity, component.IsConnected);

        // Couldn't toggle!
        if (!component.IsConnected)
            return;

        component.ConnectStream = _audio.Stop(component.ConnectStream);
        component.ConnectStream = _audio.PlayPredicted(component.ConnectSound, owner, user)?.Entity;

        UpdateUserInterface(ent);
    }

    /// <summary>
    /// Tries to retrieve the internals component of either the gas tank's user,
    /// or the gas tank's... containing container
    /// </summary>
    /// <param name="user">The user of the gas tank</param>
    /// <returns>True if internals comp isn't null, false if it is null</returns>
    private bool TryGetInternalsComp(Entity<GasTankComponent> ent, out EntityUid? internalsUid, out InternalsComponent? internalsComp, EntityUid? user = null)
    {
        internalsUid = default;
        internalsComp = default;

        // If the gas tank doesn't exist for whatever reason, don't even bother
        if (TerminatingOrDeleted(ent.Owner))
            return false;

        user ??= ent.Comp.User;
        // Check if the gas tank's user actually has the component that allows them to use a gas tank and mask
        if (TryComp<InternalsComponent>(user, out var userInternalsComp))
        {
            internalsUid = user;
            internalsComp = userInternalsComp;
            return true;
        }

        // Yeah I have no clue what this actually does, I appreciate the lack of comments on the original function
        if (_containers.TryGetContainingContainer((ent.Owner, Transform(ent.Owner)), out var container))
        {
            if (TryComp<InternalsComponent>(container.Owner, out var containerInternalsComp))
            {
                internalsUid = container.Owner;
                internalsComp = containerInternalsComp;
                return true;
            }
        }

        return false;
    }

    public void DisconnectFromInternals(Entity<GasTankComponent> ent, EntityUid? user = null)
    {
        var (owner, component) = ent;

        if (component.User == null)
            return;

        TryGetInternalsComp(ent, out var internalsUid, out var internalsComp, component.User);
        component.User = null;

        _actions.SetToggled(component.ToggleActionEntity, false);

        if (internalsUid != null && internalsComp != null)
            _internals.DisconnectTank((internalsUid.Value, internalsComp));
        component.DisconnectStream = _audio.Stop(component.DisconnectStream);
        component.DisconnectStream = _audio.PlayPredicted(component.DisconnectSound, owner, user)?.Entity;

        UpdateUserInterface(ent);
    }

    private void ToggleInternals(Entity<GasTankComponent> ent)
    {
        if (ent.Comp.IsConnected)
        {
            DisconnectFromInternals(ent);
        }
        else
        {
            ConnectToInternals(ent);
        }
    }
}
