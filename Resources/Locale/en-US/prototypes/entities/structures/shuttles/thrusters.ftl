ent-BaseThrusterDynamic = thruster
    .desc = A thruster that allows a shuttle to move.

ent-BaseThrusterStatic = stationary thruster
    .desc = A stationary thruster that allows a shuttle to move.

ent-Thruster = { ent-BaseThrusterDynamic }
    .desc = { ent-BaseThrusterDynamic.desc }

ent-ThrusterStatic = { ent-BaseThrusterStatic }
    .desc = { ent-BaseThrusterStatic.desc }

ent-DebugThruster = debug thruster
    .desc = It goes nyooooooom. It doesn't need power nor space.

ent-Gyroscope = gyroscope
    .desc = Increases the shuttle's potential angular rotation.

ent-DebugGyroscope = debug gyroscope
    .desc = { ent-Gyroscope.desc }
