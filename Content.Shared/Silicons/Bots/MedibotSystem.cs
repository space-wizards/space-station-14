using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Silicons.Bots;

/// <summary>
/// Handles emagging medibots and provides api.
/// </summary>
public sealed class MedibotSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmaggableMedibotComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<MedibotComponent, UserActivateInWorldEvent>(OnInteract);
        SubscribeLocalEvent<MedibotComponent, MedibotInjectDoAfterEvent>(OnInject);
    }

    private void OnEmagged(EntityUid uid, EmaggableMedibotComponent comp, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        if (!TryComp<MedibotComponent>(uid, out var medibot))
            return;

        foreach (var (state, treatment) in comp.Replacements)
        {
            medibot.Treatments[state] = treatment;
        }

        args.Handled = true;
    }

    private void OnInteract(Entity<MedibotComponent> medibot, ref UserActivateInWorldEvent args)
    {
        if (!CheckInjectable(medibot!, args.Target, true)) return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, 2f, new MedibotInjectDoAfterEvent(), args.User, args.Target)
        {
            BlockDuplicate = true,
            BreakOnMove = true,
        });
    }

    private void OnInject(EntityUid uid, MedibotComponent comp, ref MedibotInjectDoAfterEvent args)
    {
        if (args.Cancelled) return;

        if (args.Target is { } target)
            TryInject(uid, target);
    }

    /// <summary>
    /// Get a treatment for a given mob state.
    /// </summary>
    /// <remarks>
    /// This only exists because allowing other execute would allow modifying the dictionary, and Read access does not cover TryGetValue.
    /// </remarks>
    public bool TryGetTreatment(MedibotComponent comp, MobState state, [NotNullWhen(true)] out MedibotTreatment? treatment)
    {
        return comp.Treatments.TryGetValue(state, out treatment);
    }

    /// <summary>
    /// Checks if the target can be injected.
    /// </summary>
    public bool CheckInjectable(Entity<MedibotComponent?> medibot, EntityUid target, bool manual = false)
    {
        if (!Resolve(medibot, ref medibot.Comp, false)) return false;

        if (HasComp<NPCRecentlyInjectedComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("medibot-recently-injected"), medibot, medibot);
            return false;
        }

        if (!TryComp<MobStateComponent>(target, out var mobState)) return false;
        if (!TryComp<DamageableComponent>(target, out var damageable)) return false;
        if (!_solutionContainer.TryGetInjectableSolution(target, out _, out _)) return false;

        if (mobState.CurrentState != MobState.Alive && mobState.CurrentState != MobState.Critical)
        {
            _popup.PopupClient(Loc.GetString("medibot-target-dead"), medibot, medibot);
            return false;
        }

        var total = damageable.TotalDamage;
        if (total == 0 && !HasComp<EmaggedComponent>(medibot))
        {
            _popup.PopupClient(Loc.GetString("medibot-target-healthy"), medibot, medibot);
            return false;
        }

        if (!TryGetTreatment(medibot.Comp, mobState.CurrentState, out var treatment) || !treatment.IsValid(total) && !manual) return false;

        return true;
    }

    /// <summary>
    /// Tries to inject the target.
    /// </summary>
    public bool TryInject(Entity<MedibotComponent?> medibot, EntityUid target)
    {
        if (!Resolve(medibot, ref medibot.Comp, false)) return false;

        if (!_interaction.InRangeUnobstructed(medibot.Owner, target)) return false;

        if (!TryComp<MobStateComponent>(target, out var mobState)) return false;
        if (!TryGetTreatment(medibot.Comp, mobState.CurrentState, out var treatment)) return false;
        if (!_solutionContainer.TryGetInjectableSolution(target, out var injectable, out _)) return false;

        EnsureComp<NPCRecentlyInjectedComponent>(target);
        _solutionContainer.TryAddReagent(injectable.Value, treatment.Reagent, treatment.Quantity, out _);

        _popup.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target, target);
        _popup.PopupClient(Loc.GetString("medibot-target-injected"), medibot, medibot);

        _audio.PlayPredicted(medibot.Comp.InjectSound, medibot, medibot);

        return true;
    }
}

[Serializable, NetSerializable]
public sealed partial class MedibotInjectDoAfterEvent : SimpleDoAfterEvent { }
