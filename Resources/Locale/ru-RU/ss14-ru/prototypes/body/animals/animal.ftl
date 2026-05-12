ent-BaseMobAnimal = { "" }
    .desc = { "" }

ent-OrganAnimalMetabolizer = { "" }
    .desc = { "" }

ent-OrganAnimal = { ent-OrganBase }
    .desc = { ent-OrganBase.desc }
    .suffix = Животное

ent-OrganAnimalInternal = { ent-OrganAnimal }
    .desc = { ent-OrganAnimal.desc }
    .suffix = { ent-OrganAnimal.suffix }

