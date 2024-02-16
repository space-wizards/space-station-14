ent-PresentBase = Present
    .desc = A little box with incredible surprises inside.

ent-Present = { ent-[PresentBase, BaseStorageItem] }
    .desc = { ent-[PresentBase, BaseStorageItem].desc }
    .suffix = Empty

ent-PresentRandomUnsafe = { ent-[PresentBase, BaseItem] }
    .desc = { ent-[PresentBase, BaseItem].desc }
    .suffix = Filled Unsafe

ent-PresentRandomInsane = { ent-PresentRandomUnsafe }
    .desc = { ent-PresentRandomUnsafe.desc }
    .suffix = Filled Insane

ent-PresentRandom = { ent-[PresentBase, BaseItem] }
    .desc = { ent-[PresentBase, BaseItem].desc }
    .suffix = Filled Safe

ent-PresentTrash = Wrapping Paper
    .desc = Carefully folded, taped, and tied with a bow. Then ceremoniously ripped apart and tossed on the floor.

