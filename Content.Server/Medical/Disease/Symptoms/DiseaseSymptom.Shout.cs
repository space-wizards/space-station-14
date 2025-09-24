using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Content.Shared.Medical.Disease;
using Robust.Shared.Random;
using Content.Server.Chat.Systems;

namespace Content.Server.Medical.Disease.Symptoms;

[DataDefinition]
public sealed partial class SymptomShout : SymptomBehavior
{
    /// <summary>
    /// Dataset of localized lines to shout.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? Pack { get; private set; }

    /// <summary>
    /// If true, suppress chat window output (bubble only).
    /// </summary>
    [DataField]
    public bool HideChat { get; private set; } = true;
}

public sealed partial class SymptomShout
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    /// <summary>
    /// Makes the carrier shout a randomly picked localized line.
    /// </summary>
    public override void OnSymptom(EntityUid uid, DiseasePrototype disease)
    {
        if (!_prototypeManager.Resolve(Pack, out var pack))
            return;

        var message = Loc.GetString(_random.Pick(pack.Values));
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, HideChat);
    }
}
