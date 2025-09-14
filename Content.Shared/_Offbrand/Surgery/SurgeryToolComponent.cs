/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.Inventory;
using Content.Shared.Whitelist;

namespace Content.Shared._Offbrand.Surgery;

[RegisterComponent]
[Access(typeof(SurgeryToolSystem))]
public sealed partial class SurgeryToolComponent : Component
{
    [DataField(required: true)]
    public SlotFlags SlotsToCheck;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField(required: true)]
    public LocId SlotsDenialPopup;

    [DataField(required: true)]
    public LocId DownDenialPopup;
}
