ent-BaseThruster = { ent-BaseStructureDynamic }
    .desc = { ent-BaseStructureDynamic.desc }
    .suffix = { "" }
ent-Thruster = { ent-['BaseThruster', 'ConstructibleMachine'] }

  .desc = { ent-['BaseThruster', 'ConstructibleMachine'].desc }
  .suffix = { "" }
ent-DebugThruster = { ent-BaseThruster }
    .suffix = DEBUG
    .desc = { ent-BaseThruster.desc }
ent-Gyroscope = { ent-['BaseThruster', 'ConstructibleMachine'] }

  .desc = { ent-['BaseThruster', 'ConstructibleMachine'].desc }
  .suffix = { "" }
ent-DebugGyroscope = { ent-BaseThruster }
    .suffix = DEBUG
    .desc = { ent-BaseThruster.desc }
