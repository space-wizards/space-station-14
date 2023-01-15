ent-PresentBase = Present
    .desc = A little box with incredible surprises inside.
    .suffix = { "" }
ent-Present = { ent-['PresentBase', 'BaseStorageItem'] }

  .suffix = Empty
  .desc = { ent-['PresentBase', 'BaseStorageItem'].desc }
ent-PresentRandomUnsafe = { ent-['PresentBase', 'BaseItem'] }

  .suffix = Filled Unsafe
  .desc = { ent-['PresentBase', 'BaseItem'].desc }
ent-PresentRandomInsane = { ent-PresentRandomUnsafe }
    .suffix = Filled Insane
    .desc = { ent-PresentRandomUnsafe.desc }
ent-PresentRandom = { ent-['PresentBase', 'BaseItem'] }

  .suffix = Filled Safe
  .desc = { ent-['PresentBase', 'BaseItem'].desc }
ent-PresentTrash = Wrapping Paper
    .desc = Carefully folded, taped, and tied with a bow. Then ceremoniously ripped apart and tossed on the floor.
    .suffix = { "" }
