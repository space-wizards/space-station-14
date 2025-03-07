# Necro ent
ent-MobNecromant = иссушитель звездных душ
    .desc = От него веет смертью и загадочностью.
    .suffix = Некроморф, Призрачная роль
ent-MobSlasher = расчленитель
    .desc = Похож на мутировавший труп.
    .suffix = Некроморф, Призрачная роль
ent-MobPregnant = беременный
    .desc = Похож на мутировавший труп.
    .suffix = Некроморф, Призрачная роль
ent-MobSlasherSmall = некроморф
    .desc = Похож на мутировавший труп.
    .suffix = Некроморф, Призрачная роль
ent-MobBrute = зверь
    .desc = Поистине гигантский некроморф.
    .suffix = Некроморф, Призрачная роль
ent-MobInfector = заразитель
    .desc = Некроморф напоминающий своим внешним видом ската.
    .suffix = Некроморф, Призрачная роль
ent-MobDivader = разделитель
    .desc = Похож на мутировавший труп.
    .suffix = Некроморф, Призрачная роль
ent-MobDivaderRH = рука разделителя
    .desc = Рука некроморфа разделителя, похоже она живёт своей жизнью.
    .suffix = Некроморф
ent-MobDivaderLH = рука разделителя
    .desc = Рука некроморфа разделителя, похоже она живёт своей жизнью.
    .suffix = Некроморф
ent-MobDivaderH = голова разделителя
    .desc = Голова некроморфа разделителя, похоже она живёт своей жизнью.
    .suffix = Некроморф
ent-MobCorpseCollector = собиратель трупов
    .desc = Какой же этот некроморф огромный!
    .suffix = Некроморф, Призрачная роль
ent-MobTwitcher = охотник
    .desc = Похож на мутировавший труп.
    .suffix = Некроморф, Призрачная роль
ent-MobTwitcherlvl2 = возвышенный
    .desc = Похож на мутировавший труп.
    .suffix = Некроморф
# Other ent
ent-StructureObelisk = красный обелиск
    .desc = От него веет смертью.
ent-StructureBlackObelisk = чёрный обелиск
    .desc = От него веет смертью.
ent-StructureObelisk2 = красный обелиск
    .desc = От него веет смертью.
    .suffix = Без оповещения
ent-StructureBlackObelisk2 = чёрный обелиск
    .desc = От него веет смертью.
    .suffix = Без оповещения
ent-FloorNecroTileItemFlesh = некротический пол
    .desc = От него дурно пахнет.
ent-NecroKudzu = некроморфные волокна
    .desc = Кажется они распространяются. Не опасны и уязвимы, но замедляют передвижение.
ent-NecroTentacle = некро щупальца
    .desc = Кажется они не распространяются. Выглядят очень опасными.

# Stoper

ent-ObeliskStoper = бета001
    .desc = Прототип, созданный на основе ксеноартефактов. Останавливает пси-влияние некрообелиск на некоторое время. Одноразовый.
ent-CrateObeliskStoper = ящик с бета001
    .desc = Останавливает некрообелиск на некоторое время.

# ZetaOne

ent-CrateZetaOneMedipen = ящик с ZetaOne
    .desc = Ящик с медипенами ZetaOne лечащими некроинфецию.
ent-ZetaOneMedipen = медипен ZetaOne
    .desc = Содержит в себе лекарство от некроинфеции.
reagent-name-zetaone = ZetaOne
reagent-desc-zetaone = На вкус как лекарство.
reagent-effect-guidebook-cure-infection-dead =
    { $chance ->
        [1] Лечит
       *[other] лечат
    } некроинфецию

# ZetaTwo

ent-ZetaTwoMedipen = медипен ZetaTwo
    .desc = Содержит в себе лекарство от некроинфеции и сильно действующий наркотик импердразин, восстанавливающий рассудок после пребывание возле обелиска.
reagent-effect-guidebook-cure-sanity =
    { $chance ->
        [1] Восстанавливает
       *[other] восстанавливает
    } рассудок после пребывания возле некро-обелиска

# Extract infector

ent-SyringeExtractInfectorDead = шприц
    .desc = { ent-BaseSyringe.desc }
    .suffix = Экстракт заразителя, Не маппить
entity-name-extract-infector = экстракт заразителя
reagent-desc-extract-infector = Густой и розовый, пахнет отвратительно. Пробовать не стану!
reagent-effect-guidebook-cause-infection-dead =
    { $chance ->
        [1] Заражает
       *[other] заражает
    } некроинфецией вызывающей обращение в некроморфа после смерти

# Serum enslaved

ent-SyringeSerumEnslaved = шприц
    .desc = { ent-BaseSyringe.desc }
    .suffix = Сыворотка порабощения
entity-name-serum-enslaved = сыворотка порабощения
reagent-desc-serum-enslaved = Густой и чёрный, пахнет ужасно.
reagent-effect-guidebook-cause-enslave =
    { $chance ->
        [1] Порабощает
       *[other] порабощает
    } жертву и делает её рабом юнитологов

# Necromant store

ent-ActionNecromantArmy = призыв расчленителя.
    .desc = призыв расчленителя.
ent-ActionNecromantPregnant = призыв беременного.
    .desc = призыв беременного.
ent-ActionNecromantTwitcher = призыв охотника.
    .desc = призыв охотника.
ent-ActionNecromantInfector = призыв заразителя.
    .desc = призыв заразителя.
ent-ActionNecromantBrute = призыв зверя.
    .desc = призыв зверя.
ent-ActionNecromantDivader = призыв раздетителя.
    .desc = призыв раздетителя.
action--slasher = Призыв расчленителя.
action-description-pregnant = Призыв беременного.
action-description-twitcher = Призыв охотника.
action-description-infector = Призыв заразителя.
action-description-brute = Призыв зверя.
action-description-divader = Призыв раздетителя.
list-name-slasher = некроморф
list-name-pregnant = беременный
list-name-twitcher = охотник
list-name-infector = заразитель
list-name-brute = зверь
list-name-divader = разделитель
list-description-slasher = Этой способностью вы сможете призвать рядового некроморфа.
list-description-pregnant = Этой способностью вы сможете призвать беременного.
list-description-twitcher = Этой способностью вы сможете призвать охотника.
list-description-infector = Этой способностью вы сможете призвать заразителя.
list-description-brute = Этой способностью вы сможете призвать зверя.
list-description-divader = Этой способностью вы сможете призвать раздетителя.
entity-name-shop-necromant = Магазин способностей.
entity-description-shop-necromant = Открыть магазин способностей.

# Technology

research-technology-basic-necro-research = Некротехнологии

# Actions

ent-ActionUnitologObeliskSpawn = Призвать обелиск
    .desc = Призвать обелиск находясь рядом с тремя порабощёнными и трупом гуманоида.
ent-ActionUnitologTentacleSpawn = Разместить некро щупальца
    .desc = Разместить щупальца некро, наносящие урон, но не распространяющиеся.
