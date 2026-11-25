using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="ThiefRuleSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(ThiefRuleSystem))]
public sealed partial class ThiefRuleComponent : Component;
