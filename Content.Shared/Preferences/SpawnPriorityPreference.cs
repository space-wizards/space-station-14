// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.Preferences
{
    /// <summary>
    /// The spawn priority preference for a profile. Stored in database!
    /// </summary>
    public enum SpawnPriorityPreference
    {
        ///////////////////////
        /// DO NOT TOUCH!!! ///
        ///////////////////////
        None = 0,
        Arrivals = 1,
        Cryosleep = 2,
    }
}
