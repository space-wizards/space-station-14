// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 mkanke-real <mikekanke@gmail.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using System.Text;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.BloodCult.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BloodCultRuneCarverComponent : Component
{
	// TODO: Switch from using this static list of valid runes to a dynamic list stored in the BloodCultistComponent, which
	// can be accessed from both the server and from the client UI code
	public static string[] ValidRunes = {
		"BarrierRune", "EmpoweringRune", "OfferingRune", "ReviveRune", "SummoningRune", "TearVeilRune"//,
		//"BloodBoilRune", "SpiritRealmRune", "TeleportRune"
	};

	/// <summary>
    ///     The entity to spawn (e.g. animation) while carving.
    /// </summary>
    [DataField, AutoNetworkedField] public string InProgress = "PuddleSparkle";

	/// <summary>
    ///     The entity to spawn when used on self.
    /// </summary>
    [DataField, AutoNetworkedField] public string Rune = "";

	/// <summary>
    ///     Blood damage to apply to self when used to carve a rune.
    /// </summary>
    [DataField] public int BleedOnCarve = 5;

	/// <summary>
    ///     Time in seconds needed to carve a rune.
    /// </summary>
    [DataField] public float TimeToCarve = 15f;

	/// <summary>
    ///     Sound that plays when used to carve a rune.
    /// </summary>
    [DataField] public SoundSpecifier CarveSound = new SoundCollectionSpecifier("gib");

	/// <summary>
	/// 	Current user using the knife
	/// </summary>
	[ViewVariables]
	public EntityUid? User;
}

[Serializable, NetSerializable]
public sealed class RuneUserInterfaceState : BoundUserInterfaceState
{
	public readonly string Rune;

	public RuneUserInterfaceState(string rune)
	{
		Rune = rune;
	}
}

[Serializable, NetSerializable]
public sealed class RunesMessage : BoundUserInterfaceMessage
{
	public string ProtoId;

	public RunesMessage(string protoId)
	{
		ProtoId = protoId;
	}
}

[Serializable, NetSerializable]
public enum RunesUiKey : byte
{
	Key
}
