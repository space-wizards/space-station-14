// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.Laws;

public abstract partial class SharedSiliconLawSystem
{
    private void InitializeUpdater()
    {
        SubscribeLocalEvent<SiliconLawUpdaterComponent, EntInsertedIntoContainerMessage>(OnUpdaterInsert);
    }

    protected virtual void OnUpdaterInsert(Entity<SiliconLawUpdaterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // TODO: Prediction
    }
}
