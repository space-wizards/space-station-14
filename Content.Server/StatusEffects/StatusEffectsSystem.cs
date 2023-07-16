using Content.Server.Administration;
using Content.Shared.StatusEffects;
using Content.Shared.StatusEffects.Components;
using Content.Shared.Administration;
using Content.Shared.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Timing;

namespace Content.Server.StatusEffects;

/// <inheritdoc/>
public sealed partial class StatusEffectsSystem : SharedStatusEffectsSystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _sharedSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectsComponent, ComponentShutdown>(OnShutdown);

        _consoleHost.RegisterCommand("addeffect",
            Loc.GetString("add-effect-command"),
            "addeffect <uid> <effect ID> <strength> <timer>",
            AddEffectCommand,
            StatusCommandCompletion);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StatusEffectComponent>();

        var curTime = Timing.CurTime;

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.IsTimed && curTime >= comp.Length.End)
                QueueDel(uid);
        }
    }

    private void OnShutdown(EntityUid uid, StatusEffectsComponent component, ComponentShutdown args)
    {
        if (component.StatusContainer == null)
            return;

        foreach (var effectUid in component.StatusContainer.ContainedEntities)
        {
            QueueDel(effectUid);
        }
    }

    [AdminCommand(AdminFlags.Fun)]
    private void AddEffectCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError("Too few arguments, arguments that can be used (in order) are: Entity Uid, Effect Prototype, optionally the strength of the effect and the length in seconds.");
            return;
        }

        if (!EntityUid.TryParse(args[0], out var uid) || !HasComp<StatusEffectsComponent>(uid))
        {
            shell.WriteError("Entity either doesn't exist or cannot have effects.");
            return;
        }

        if (!PrototypeManager.TryIndex<EntityPrototype>(args[1], out var effectPrototype) || !effectPrototype.HasComponent<StatusEffectComponent>())
        {
            shell.WriteError("Prototype either isn't real or doesn't have StatusEffectComponent.");
            return;
        }

        var strength = 1;
        TimeSpan? length = null;

        if (args.TryGetValue(2, out var strStrength) && int.TryParse(strStrength, out var newStrength))
            strength = newStrength;

        if (args.TryGetValue(3, out var strLength) && float.TryParse(strLength, out var newLength))
            length = TimeSpan.FromSeconds(newLength);

        _sharedSystem.ApplyEffect(uid, effectPrototype.ID, strength, length, false, true);
    }

    private CompletionResult StatusCommandCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint("<uid>");

        if (args.Length == 2)
            return CompletionResult.FromHint("<effect proto ID>");

        if (args.Length == 3)
            return CompletionResult.FromHint("<strength>");

        if (args.Length == 4)
            return CompletionResult.FromHint("<length>");

        return CompletionResult.Empty;
    }
}
