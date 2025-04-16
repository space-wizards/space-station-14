using Content.Server.GameTicking.Rules.VariationPass;
using Content.Server._Moffstation.GameTicking.Rules.VariationPass.Components;
using Content.Server._Moffstation.GameTicking.Rules.VariationPass.Components.ReplacementMarkers;

namespace Content.Server._Moffstation.GameTicking.Rules.VariationPass;

/// <summary>
/// This handles the ability to replace entities marked with <see cref="SolarPanelReplacementMarkerComponent"/> in a variation pass
/// </summary>
public sealed class SolarPanelReplaceVariationPassSystem : BaseEntityReplaceVariationPassSystem<SolarPanelReplacementMarkerComponent, SolarPanelReplaceVariationPassComponent>
{
}
