ent-BaseGenerator = generator
    .desc = A high efficiency thermoelectric generator.

ent-BaseGeneratorWallmount = wallmount generator
    .desc = A high efficiency thermoelectric generator stuffed in a wall cabinet.

ent-BaseGeneratorWallmountFrame = wallmount generator frame
    .desc = A construction frame for a wallmount generator.

ent-GeneratorBasic = { ent-BaseGenerator }
    .desc = { ent-BaseGenerator.desc }
    .suffix = Basic, 3kW

ent-GeneratorBasic15kW = { ent-BaseGenerator }
    .desc = { ent-BaseGenerator.desc }
    .suffix = Basic, 15kW

ent-GeneratorWallmountBasic = { ent-BaseGeneratorWallmount }
    .desc = { ent-BaseGeneratorWallmount.desc }
    .suffix = Basic, 3kW

ent-GeneratorWallmountAPU = shuttle APU
    .desc = An auxiliary power unit for a shuttle - 6kW.
    .suffix = APU, 6kW

ent-GeneratorRTG = RTG
    .desc = A Radioisotope Thermoelectric Generator for long term power.
    .suffix = 10kW

ent-GeneratorRTGDamaged = damaged RTG
    .desc = A Radioisotope Thermoelectric Generator for long term power. This one has damaged shielding.
    .suffix = 10kW

