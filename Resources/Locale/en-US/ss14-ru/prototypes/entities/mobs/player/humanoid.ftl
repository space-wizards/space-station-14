ent-RandomHumanoidSpawnerDeathSquad = Агент Эскадрона смерти
    .desc = { "" }
    .suffix = Роль ОБР, Эскадрон смерти

# ERT Leader
ent-RandomHumanoidSpawnerERTLeader = ОБР лидер
    .suffix = Роль ОБР, Базовый
    .desc = { "" }
ent-RandomHumanoidSpawnerERTLeaderEVA = ОБР лидер
    .suffix = Роль ОБР, ВКД
    .desc = { ent-RandomHumanoidSpawnerERTLeader.desc }
ent-RandomHumanoidSpawnerERTLeaderArmed = { ent-RandomHumanoidSpawnerERTLeaderEVA }
    .suffix = Роль ОБР, Вооружен, ВКД
    .desc = Вооружен XL8, 4 запасных магазина разного типа.

# ERT Chaplain
ent-RandomHumanoidSpawnerERTChaplain = ОБР священник
    .desc = { ent-RandomHumanoidSpawnerERTLeader.desc }
    .suffix = Роль ОБР, Базовый
ent-RandomHumanoidSpawnerERTChaplainEVA = ОБР священник
    .suffix = Роль ОБР, ВКД
    .desc = { ent-RandomHumanoidSpawnerERTChaplain.desc }

# ERT Janitor
ent-RandomHumanoidSpawnerERTJanitor = ОБР уборщик
    .desc = { ent-RandomHumanoidSpawnerERTLeader.desc }
    .suffix = Роль ОБР, Базовый
    .desc = { ent-RandomHumanoidSpawnerERTLeader.desc }
ent-RandomHumanoidSpawnerERTJanitorEVA = ОБР уборщик
    .suffix = Роль ОБР, ВКД
    .desc = { ent-RandomHumanoidSpawnerERTJanitor.desc }

# ERT Engineer
ent-RandomHumanoidSpawnerERTEngineer = ОБР инженер
    .desc = { ent-RandomHumanoidSpawnerERTLeader.desc }
    .suffix = Роль ОБР, Базовый
    .desc = { ent-RandomHumanoidSpawnerERTLeader.desc }
ent-RandomHumanoidSpawnerERTEngineerEVA = { ent-RandomHumanoidSpawnerERTEngineer }
    .suffix = Роль ОБР, ВКД
    .desc = { ent-RandomHumanoidSpawnerERTEngineer.desc }
ent-RandomHumanoidSpawnerERTEngineerArmed = { ent-RandomHumanoidSpawnerERTEngineer }
    .suffix = Роль ОБР, Вооружен, ВКД
    .desc = Вооружен Силовиком, имеет детонационный шнур и коробку детонаторов.

# ERT Security
ent-RandomHumanoidSpawnerERTSecurity = ОБР офицер безопасности
    .desc = { ent-RandomHumanoidSpawnerERTLeader.desc }
    .suffix = Роль ОБР, Базовый
ent-RandomHumanoidSpawnerERTSecurityEVA = ОБР офицер безопасности
    .suffix = Роль ОБР, ВКД
    .desc = { ent-RandomHumanoidSpawnerERTSecurity.desc }
ent-RandomHumanoidSpawnerERTSecurityArmedRifle = { ent-RandomHumanoidSpawnerERTSecurityEVA }, Стрелок
    .suffix = Роль ОБР, Вооружен, ВКД
    .desc = Вооружен Лектером, 4 запасных магазина различного типа, Лазерная пушка и переносной зарядник.
ent-RandomHumanoidSpawnerERTSecurityArmedGrenade = { ent-RandomHumanoidSpawnerERTSecurityEVA }, Гренадер
    .suffix = Роль ОБР, Вооружен, ВКД
    .desc = Вооружен Гидрой с осколочными снарядами, имеет в запасе 6 фугасных, 3 ЭМИ и светошумовых снаряда.
ent-RandomHumanoidSpawnerERTSecurityArmedVanguard = { ent-RandomHumanoidSpawnerERTSecurityEVA }, Авангард
    .suffix = Роль ОБР, Вооружен, ВКД
    .desc = Вооружен WT550, 4 запасных магазина, 3 телескопических щита.
ent-RandomHumanoidSpawnerERTSecurityArmedShotgun = { ent-RandomHumanoidSpawnerERTSecurityEVA }, Сапёр
    .suffix = Роль ОБР, Вооружен, ВКД
    .desc = Вооружен Силовиком, 3 коробки различной дроби, осколочной гранатой, детонационным шнуром и коробкой детонаторов.

# ERT Medic
ent-RandomHumanoidSpawnerERTMedical = ОБР медик
    .desc = { ent-RandomHumanoidSpawnerERTLeader.desc }
    .suffix = Роль ОБР, Базовый
    .desc = { ent-RandomHumanoidSpawnerERTLeader.desc }
ent-RandomHumanoidSpawnerERTMedicalEVA = ОБР медик
    .suffix = Роль ОБР, ВКД
    .desc = { ent-RandomHumanoidSpawnerERTMedical.desc }
ent-RandomHumanoidSpawnerERTMedicalArmed = ОБР медик
    .suffix = Роль ОБР, Вооружен, ВКД
    .desc = Вооружен Лектером, 4 запасных магазина разного типа.

# CBURN
ent-RandomHumanoidSpawnerCBURNUnit = Агент РХБЗЗ
    .desc = { "" }
    .suffix = Роль ОБР
    .desc = { "" }

# misc
ent-RandomHumanoidSpawnerCentcomOfficial = Представитель Центком
    .desc = { "" }
ent-RandomHumanoidSpawnerSyndicateAgent = Агент Синдиката
    .desc = { "" }
ent-RandomHumanoidSpawnerNukeOp = Ядерный оперативник
    .desc = { "" }
ent-RandomHumanoidSpawnerCluwne = Клувень
    .desc = { "" }
    .suffix = Спавнит клувеня
ent-RandomHumanoidSpawnerERTLeaderEVALecter = { ent-RandomHumanoidSpawnerERTLeaderEVA }
    .suffix = Роль ОБР, Лектер, ВКД
    .desc = { ent-RandomHumanoidSpawnerERTLeaderEVA.desc }
ent-RandomHumanoidSpawnerERTSecurityEVALecter = { ent-RandomHumanoidSpawnerERTSecurityEVA }
    .suffix = Роль ОБР, Лектер, ВКД
    .desc = { ent-RandomHumanoidSpawnerERTSecurityEVA.desc }
