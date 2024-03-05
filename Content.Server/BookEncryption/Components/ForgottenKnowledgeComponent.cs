using Content.Shared.DeviceLinking;
using Content.Shared.BookEncryption;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.BookEncryption.Components;

/// <summary>
/// The underlying system that manages encrypted knowledge.
/// A component storing randomized pairs to keywords.
/// </summary>
[RegisterComponent, Access(typeof(ForgottenKnowledgeSystem))]
public sealed partial class ForgottenKnowledgeComponent : Component
{
    [DataField]
    public Dictionary<EncryptedBookDisciplinePrototype, List<(string, string)>> KeywordPairs = new();
}
