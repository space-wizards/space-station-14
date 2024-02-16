ent-BaseThruster = { ent-BaseStructureDynamic }
    .desc = { ent-BaseStructureDynamic.desc }

ent-Thruster = thruster
    .desc = { ent-[ BaseThruster, ConstructibleMachine ].desc }

ent-ThrusterUnanchored = { ent-Thruster }
    .desc = { ent-Thruster.desc }
    .suffix = Unanchored

ent-DebugThruster = { ent-BaseThruster }
    .desc = { ent-BaseThruster.desc }
    .suffix = DEBUG

ent-Gyroscope = gyroscope
    .desc = { ent-[ BaseThruster, ConstructibleMachine ].desc }

ent-GyroscopeUnanchored = { ent-Gyroscope }
    .desc = { ent-Gyroscope.desc }
    .suffix = Unanchored

ent-DebugGyroscope = { ent-BaseThruster }
    .desc = { ent-BaseThruster.desc }
    .suffix = DEBUG

