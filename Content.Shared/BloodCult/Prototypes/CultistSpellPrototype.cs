// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.BloodCult.Prototypes;

[Prototype("cultAbility")]
public sealed partial class CultAbilityPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    ///     What event should be raised
    /// </summary>
    [DataField] public object? Event;

    /// <summary>
    ///     What actions should be given
    /// </summary>
    [DataField] public List<EntProtoId>? ActionPrototypes;

	/// <summary>
	///		Health drain to prepare this spell.
	/// </summary>
	[DataField] public int HealthDrain = 7;

	/// <summary>
	///		Length of DoAfter to carve this spell.
	/// </summary>
	[DataField] public int DoAfterLength = 5;

	/// <summary>
    ///     Sound that plays when used to carve a spell.
    /// </summary>
    [DataField] public SoundSpecifier CarveSound = new SoundCollectionSpecifier("gib");
}
