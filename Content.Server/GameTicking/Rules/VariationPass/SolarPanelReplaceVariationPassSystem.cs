// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Server.GameTicking.Rules.VariationPass.Components.ReplacementMarkers;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <summary>
/// This handles the ability to replace entities marked with <see cref="SolarPanelReplacementMarkerComponent"/> in a variation pass
/// </summary>
public sealed class SolarPanelReplaceVariationPassSystem : BaseEntityReplaceVariationPassSystem<SolarPanelReplacementMarkerComponent, SolarPanelReplaceVariationPassComponent>
{
}
