// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using System.Collections.Frozen;

namespace Content.Shared.BloodCult
{
	/// <summary>
	/// Constants and shared data for the Blood Cult system
	/// </summary>
	public static class BloodCultConstants
	{
		/// <summary>
		/// All reagent prototype IDs that count as blood for ritual sacrifice purposes.
		/// This includes standard blood types from all species.
		/// </summary>
		public static readonly FrozenSet<string> SacrificeBloodReagents = new[]
		{
			"Blood",
			"Slime",
			"Sap",
			"InsectBlood",
			"CopperBlood",
			"AmmoniaBlood",
			"ZombieBlood",
			"ChangelingBlood"
		}.ToFrozenSet();
	}
}

