using Content.Server.Body.Systems;
using Content.Server.Database;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Speech.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CD.Traits;

public sealed class SynthSystem : EntitySystem
{
    // Begin DeltaV - make strings static readonly
    private static readonly ProtoId<TypingIndicatorPrototype> RobotTypingIndicator = "robot";
    private static readonly ProtoId<ReagentPrototype> SynthBloodReagent = "SynthBlood";
    // End DeltaV

    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SynthComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, SynthComponent component, ComponentStartup args)
    {
        if (TryComp<TypingIndicatorComponent>(uid, out var indicator))
        {
            indicator.TypingIndicatorPrototype = RobotTypingIndicator; // DeltaV - make strings static readonly
            Dirty(uid, indicator);
        }

        // Give them synth blood. Ion storm notif is handled in that system
        _bloodstream.ChangeBloodReagent(uid, SynthBloodReagent); // DeltaV - make strings static readonly

        // Gives them the DamagedSiliconAccent component
        EnsureComp<DamagedSiliconAccentComponent>(uid, out var accent);
        accent.EnableChargeCorruption = false; //Disables corruption on low battery. This would always be active since non-silicons don't have a battery
        accent.DamageAtMaxCorruption = 200; //This is makes it usable for anyone not a silicon
        Dirty(uid, accent);
    }
}