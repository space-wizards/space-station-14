ent-BaseDefibrillator = defibrillator
    .desc = CLEAR! Zzzzat!

ent-Defibrillator = { ent-[ BaseDefibrillator, PowerCellSlotMediumItem ] }
    .desc = { ent-[ BaseDefibrillator, PowerCellSlotMediumItem ].desc }

ent-DefibrillatorEmpty = { ent-Defibrillator }
    .desc = { ent-Defibrillator.desc }
    .suffix = Empty

ent-DefibrillatorOneHandedUnpowered = { ent-BaseDefibrillator }
    .desc = { ent-BaseDefibrillator.desc }
    .suffix = One-Handed, Unpowered

