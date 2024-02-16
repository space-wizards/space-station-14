ent-BaseSubstation = substation
    .desc = Reduces the voltage of electricity put into it.

ent-BaseSubstationWall = wallmount substation
    .desc = A substation designed for compact shuttles and spaces.

ent-SubstationBasic = { ent-BaseSubstation }
    .desc = { ent-BaseSubstation.desc }
    .suffix = Basic, 2.5MJ

ent-SubstationBasicEmpty = { ent-SubstationBasic }
    .desc = { ent-SubstationBasic.desc }
    .suffix = Empty

ent-SubstationWallBasic = { ent-BaseSubstationWall }
    .desc = { ent-BaseSubstationWall.desc }
    .suffix = Basic, 2MJ

ent-BaseSubstationWallFrame = wallmount substation frame
    .desc = A substation frame for construction

