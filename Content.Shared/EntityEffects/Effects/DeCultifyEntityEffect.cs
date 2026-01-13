// SPDX-FileCopyrightText: 2025 Drywink <hugogrethen@gmail.com>
// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using System.Text.Json.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.EntityEffects;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Decultifies a Blood Cultist by increasing their DeCultification value.
/// </summary>
public sealed partial class DeCultify : EntityEffectBase<DeCultify>
{
	/// <summary>
	/// Decultification to apply every cycle.
	/// </summary>
	[DataField(required: true)]
	[JsonPropertyName("amount")]
	public float Amount = default!;

	public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
	{
		return "In large quantities, can free a person's mind from servitude to an eldritch entity.";
	}
}
