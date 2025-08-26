using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Ronstation.GameTicking.Rules.Components;

/// <summary>
/// Game rule for vampires.
/// </summary>
[RegisterComponent, Access(typeof(VampireRuleSystem))]
public sealed partial class VampireRuleComponent : Component;