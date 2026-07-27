// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Collections.Generic;

namespace Content.Shared.DeadSpace.StationAi;

[RegisterComponent]
public sealed partial class BodyCameraVisionComponent : Component
{
    public readonly HashSet<EntityUid> Sources = new();
    public bool AddedVisionComponent;
    public bool OriginalEnabled;
    public bool OriginalOccluded;
    public float OriginalRange;
}
