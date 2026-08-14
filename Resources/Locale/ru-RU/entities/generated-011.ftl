ent-BaseControllable = { "" }
    .desc = { "" }

ent-BaseMob = { ent-BaseControllable }
    .desc = { ent-BaseControllable.desc }

ent-MobPolymorphable = { "" }
    .desc = { "" }

ent-MobDamageable = { "" }
    .desc = { "" }

ent-MobCombat = { "" }
    .desc = { "" }

ent-MobAtmosExposed = { "" }
    .desc = { "" }

ent-MobAtmosStandard = { ent-MobAtmosExposed }
    .desc = { ent-MobAtmosExposed.desc }

ent-MobFlammable = { "" }
    .desc = { "" }

ent-MobRespirator = { "" }
    .desc = { "" }

ent-MobBloodstream = { "" }
    .desc = { "" }

ent-MobRandomServiceCorpse = { ent-SalvageHumanCorpse }
    .desc = { ent-SalvageHumanCorpse.desc }
    .suffix = Мёртвый, Сервис

ent-MobRandomEngineerCorpse = { ent-SalvageHumanCorpse }
    .desc = { ent-SalvageHumanCorpse.desc }
    .suffix = Мёртвый, Инженер

ent-MobRandomCargoCorpse = { ent-SalvageHumanCorpse }
    .desc = { ent-SalvageHumanCorpse.desc }
    .suffix = Мёртвый, Снабжение

ent-MobRandomMedicCorpse = { ent-SalvageHumanCorpse }
    .desc = { ent-SalvageHumanCorpse.desc }
    .suffix = Мёртвый, Медик

ent-MobRandomScienceCorpse = { ent-SalvageHumanCorpse }
    .desc = { ent-SalvageHumanCorpse.desc }
    .suffix = Мёртвый, Учёный

ent-MobRandomSecurityCorpse = { ent-SalvageHumanCorpse }
    .desc = { ent-SalvageHumanCorpse.desc }
    .suffix = Мёртвый, Служба Безопасности

ent-MobRandomCommandCorpse = { ent-SalvageHumanCorpse }
    .desc = { ent-SalvageHumanCorpse.desc }
    .suffix = Мёртвый, Командование

ent-BaseBorgChassisNotIonStormable = киборг
    .desc = Гибрид машины и человека, помогающий в работе станции. Они обожают, когда их снова и снова просят назвать свои законы.

ent-BaseBorgChassis = { ent-BaseBorgChassisNotIonStormable }
    .desc = { ent-BaseBorgChassisNotIonStormable.desc }

ent-BaseBorgTransponder = { "" }
    .desc = { "" }

ent-BaseXenoborgTransponder = { ent-BaseBorgTransponder }
    .desc = { ent-BaseBorgTransponder.desc }

ent-BaseBorgChassisNT = { ent-BaseBorgChassis }
    .desc = { ent-BaseBorgChassis.desc }

ent-BaseBorgChassisSyndicate = { ent-BaseBorgChassis }
    .desc = { ent-BaseBorgChassis.desc }

ent-BaseBorgChassisDerelict = { ent-BaseBorgChassis }
    .desc = { ent-BaseBorgChassis.desc }

ent-BaseBorgChassisSyndicateDerelict = { ent-BaseBorgChassis }
    .desc = { ent-BaseBorgChassis.desc }

ent-BaseXenoborgChassis = ксеноборг
    .desc = Гибрид машины и человека, стремящийся к самовоспроизведению. Они любят извлекать мозги и вставлять их в новые шасси ксеноборгов, чтобы пополнять свою армию.

ent-BorgChassisSelectable = { ent-BaseBorgChassisNT }
    .desc = { ent-BaseBorgChassisNT.desc }

ent-BorgChassisGeneric = обычный киборг
    .desc = { ent-BorgChassisSelectable.desc }
    .suffix = Выбранный тип

ent-BorgChassisMining = киборг-шахтёр
    .desc = { ent-BorgChassisSelectable.desc }

ent-BorgChassisEngineer = киборг-инженер
    .desc = { ent-BorgChassisSelectable.desc }

ent-BorgChassisJanitor = киборг-уборщик
    .desc = { ent-BorgChassisSelectable.desc }

ent-BorgChassisMedical = киборг-доктор
    .desc = { ent-BorgChassisSelectable.desc }

ent-BorgChassisService = киборг-официант
    .desc = { ent-BorgChassisSelectable.desc }

ent-BorgChassisSyndicateAssault = штурмовой киборг Синдиката
    .desc = Машина для убийств с доступом к различным смертоносным модулям.

ent-BorgChassisSyndicateMedical = медицинский киборг Синдиката
    .desc = Боевой медицинский киборг. Имеет ограниченный наступательный потенциал, но с лихвой компенсирует его своими вспомогательными возможностями.

ent-BorgChassisSyndicateSaboteur = саботажный киборг Синдиката
    .desc = Изящный инженерный киборг, оснащённый модулями скрытности. Проектор-хамелеон позволяет ему маскироваться под киборга Nanotrasen.

ent-BorgChassisDerelict = брошенный киборг
    .desc = Гибрид человека и машины, помогающий в работе станции. Этот находится в очень запущенном состоянии.

ent-EngineeringBorgChassisDerelict = брошенный киборг-инженер
    .desc = Гибрид человека и машины, помогающий инженерному отделу. На его поверхности видны куски странных кристаллов.

ent-JanitorBorgChassisDerelict = брошенный киборг-уборщик
    .desc = Гибрид человека и машины, помогающий сервисному отделу. Выглядит как большой бардак, больше чем всё, что он может убрать.

ent-MedicalBorgChassisDerelict = брошенный киборг-доктор
    .desc = Гибрид человека и машины, помогающий медицинскому отделу. Его иглы выглядят не очень стерильно.

ent-MiningBorgChassisDerelict = брошенный киборг-шахтёр
    .desc = Гибрид человека и машины, помогающий отделу снабжения. Этот увидел не ту сторону гибтонита.

ent-SyndicateAssaultBorgChassisDerelict = брошенный штурмовой киборг Синдиката
    .desc = Ловкая, жестокая, убивающая машина с доступом к разнообразным смертоносным модулям. Этот больше ржаво-оранжевый, чем кроваво-красный.

ent-XenoborgEngi = инженерный ксеноборг
    .desc = Гибрид машины и человека, стремящийся к самовоспроизведению. Они любят извлекать мозги и вставлять их в новые шасси ксеноборгов, чтобы пополнять свою армию. Этот, похоже, инженерного типа, так как у него больше интрументов для поддержки других ксеноборгов.
    .suffix = с мозгом

ent-XenoborgHeavy = тяжёлый ксеноборг
    .desc = Гибрид машины и человека, стремящийся к самовоспроизведению. Они любят извлекать мозги и вставлять их в новые шасси ксеноборгов, чтобы пополнять свою армию. У этого типа тяжёлые имеются  лазеры и он покрыт тяжёлыми бронепластинами. Может он и крепче, но его скорость соответствует его мощи.
    .suffix = с мозгом

ent-XenoborgScout = разведчик ксеноборг
    .desc = Гибрид машины и человека, стремящийся к самовоспроизведению. Они любят извлекать мозги и вставлять их в новые шасси ксеноборгов, чтобы пополнять свою армию. Он разведывательного типа. Его манёвренные двигатели позволяют ему быстро перемещаться в космосе.
    .suffix = с мозгом

ent-XenoborgStealth = скрытный ксеноборг
    .desc = Гибрид машины и человека, стремящийся к самовоспроизведению. Они любят извлекать мозги и вставлять их в новые шасси ксеноборгов, чтобы пополнять свою армию. Это скрытый тип. Его броня переливается на свету, как никакой другой материал, который вы видели.
    .suffix = с мозгом

ent-XenoborgEngiPrinted = { ent-XenoborgEngi }
    .desc = { ent-XenoborgEngi.desc }
    .suffix = без мозга

ent-XenoborgHeavyPrinted = { ent-XenoborgHeavy }
    .desc = { ent-XenoborgHeavy.desc }
    .suffix = без мозга

ent-XenoborgScoutPrinted = { ent-XenoborgScout }
    .desc = { ent-XenoborgScout.desc }
    .suffix = без мозга

ent-XenoborgStealthPrinted = { ent-XenoborgStealth }
    .desc = { ent-XenoborgStealth.desc }
    .suffix = без мозга

ent-MobDebugCounter = debug counter
    .desc = He can count
    .suffix = AI, DEBUG

ent-MobDebugRandomCounter = debug random counter
    .desc = He can randomize
    .suffix = AI, DEBUG

ent-MobDebugRandomLess = debug random less
    .desc = He can lessing
    .suffix = AI, DEBUG

ent-MobBat = летучая мышь
    .desc = В одних культурах они вызывают ужас, в других — хрустят на зубах.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobBee = пчела
    .desc = Приятно иметь, но нельзя построить цивилизацию на фундаменте из одного только мёда.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobAngryBee = пчела
    .desc = Какая милая пчёлка. О нет, она выглядит злой и хочет мою пиццу.
    .suffix = Злой

ent-MobChicken = курица
    .desc = Была раньше яйца, динозавром!
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobChicken1 = { ent-MobChicken }
    .desc = { ent-MobChicken.desc }
    .suffix = { ent-MobChicken.suffix }

ent-MobChicken2 = { ent-MobChicken }
    .desc = { ent-MobChicken.desc }
    .suffix = { ent-MobChicken.suffix }

ent-FoodEggChickenFertilized = { ent-FoodEgg }
    .desc = { ent-FoodEgg.desc }
    .suffix = Оплодотворённый, Курица

ent-MobCockroach = таракан
    .desc = Эта станция просто кишит насекомыми.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobGlockroach = ТТаракан
    .desc = Эта станция просто кишит на- О БОЖЕ, У ЭТОГО ТАРАКАНА ПИСТОЛЕТ!!!
    .suffix = Адмемы

ent-MobMothroach = таракамоль
    .desc = Очаровательный результат многочисленных попыток генетического смешения молей с тараканами.
    .suffix = { ent-MobCockroach.suffix }

ent-MobDuckMallard = кряква
    .desc = Очаровательная кряква, она пушистая и мягкая!
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobDuckWhite = белая утка
    .desc = Очаровательная белая уточка, она пушистая и мягкая!
    .suffix = { ent-MobDuckMallard.suffix }

ent-MobDuckBrown = коричневая утка
    .desc = Очаровательная коричневая уточка, пушистая и мягкая!
    .suffix = { ent-MobDuckMallard.suffix }

ent-FoodEggDuckFertilized = { ent-FoodEgg }
    .desc = { ent-FoodEgg.desc }
    .suffix = Оплодотворённый, Утка

ent-MobButterfly = бабочка
    .desc = Вопреки заблуждениям, это не душа вашей бабушки.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobCow = корова
    .desc = Муу.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobCrab = краб
    .desc = В народе говорят, что его клешня вышибает дух из космонавтов за грубые высказывания. Будьте вежливы и терпимы для вашей же безопасности.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobGoat = коза
    .desc = Её позвоночник состоит из длинных острых сегментов, неудивительно, что она такая ворчливая.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobSheep = овца
    .desc = Очаровательная генетически модифицированная химическая фабрика, производящая молоко и хлопок.

ent-MobSheepRainbow = радужная овца
    .desc = Овца. Эта кажется... светящейся.

ent-MobGoose = гусь
    .desc = Его желудок и разум — загадка, недоступная человеческому пониманию.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobGorilla = горилла
    .desc = Крушит, ревёт, выглядит круто. Не стойте рядом с ней.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobKangaroo = кенгуру
    .desc = Крупное сумчатое травоядное. У него мощные задние лапы с ногтями, напоминающими длинные когти.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobBoxingKangaroo = кенгуру-боксёр
    .desc = { ent-MobKangaroo.desc }
    .suffix = { ent-MobKangaroo.suffix }

ent-MobBaseAncestor = генетический предок
    .desc = Генетический бипедальный предок... э-э... чего-то. Да, на станции определённо есть что-то, произошедшее от этого, чем бы оно ни было.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobMonkey = обезьяна
    .desc = Новая церковь неодарвинистов действительно верит, что КАЖДОЕ животное произошло от обезьяны. На вкус они как свинина, а убивать их весело и приятно.
    .suffix = { ent-MobBaseAncestor.suffix }

ent-MobBaseSyndicateMonkey = обезьяна
    .desc = Новая церковь неодарвинистов действительно верит, что КАЖДОЕ животное произошло от обезьяны. На вкус они как свинина, а убивать их весело и приятно.
    .suffix = Синдикат, Базовый

ent-MobMonkeySyndicateAgent = { ent-MobBaseSyndicateMonkey }
    .desc = { ent-MobBaseSyndicateMonkey.desc }
    .suffix = Синдикат

ent-MobMonkeySyndicateAgentNukeops = { ent-MobBaseSyndicateMonkey }
    .desc = { ent-MobBaseSyndicateMonkey.desc }
    .suffix = Ядерные Оперативники

ent-MobBaseKobold = кобольд
    .desc = Двоюродные братья разумного вида унатхов, кобольды сливаются со своей естественной средой обитания, и так же противны, как обезьяны. Они готовы вырвать ваши волосы и заколоть вас до смерти.
    .suffix = { ent-MobBaseAncestor.suffix }

ent-MobKobold = кобольд
    .desc = Двоюродные братья разумного вида унатхов, кобольды сливаются со своей естественной средой обитания, и так же противны, как обезьяны. Они готовы вырвать ваши волосы и заколоть вас до смерти.
    .suffix = { ent-MobBaseKobold.suffix }

ent-MobBaseSyndicateKobold = { ent-MobBaseKobold }
    .desc = { ent-MobBaseKobold.desc }
    .suffix = Синдикат, Базовый

ent-MobKoboldSyndicateAgent = { ent-MobBaseSyndicateKobold }
    .desc = { ent-MobBaseSyndicateKobold.desc }
    .suffix = Агент Синдиката

ent-MobKoboldSyndicateAgentNukeops = { ent-MobBaseSyndicateKobold }
    .desc = { ent-MobBaseSyndicateKobold.desc }
    .suffix = Ядерные оперативники

ent-MobGuidebookMonkey = тренировочная обезьяна
    .desc = Специально обученная обезьяна, чья единственная цель в жизни — чтобы вы кликнули на неё. Можно ли считать, что обезьяна преподала вам урок?
    .suffix = { ent-MobMonkey.suffix }

ent-MobMouse = мышь
    .desc = Пии!
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobMouseDead = мышь
    .desc = Пии!
    .suffix = Мёртвый

ent-MobMouse1 = { ent-MobMouse }
    .desc = { ent-MobMouse.desc }
    .suffix = { ent-MobMouse.suffix }

ent-MobMouse2 = { ent-MobMouse }
    .desc = { ent-MobMouse.desc }
    .suffix = { ent-MobMouse.suffix }

ent-MobMouseCancer = раковая мышь
    .desc = Токсичная. Скуик!
    .suffix = { ent-MobMouse.suffix }

ent-MobLizard = ящерица
    .desc = Безобидный дракон.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobSlug = слизняк
    .desc = И они называли это ящерицей?
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobFrog = лягушка
    .desc = Прыг прыг прыг. Выглядит мокрой.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobParrotBase = попугай
    .desc = Проникает в ваши владения, шпионит за вами, и при этом остаётся классным питомцем.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobParrot = { ent-MobParrotBase }
    .desc = { ent-MobParrotBase.desc }
    .suffix = { ent-MobParrotBase.suffix }

ent-MobPenguin = пингвин
    .desc = Их жизнь — это постоянная боль из-за коленных суставов внутри тела.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobGrenadePenguin = пингвин гренадёр
    .desc = Маленький пингвин с гранатой на шее. Заготавливается Синдикатом на неблагоприятных ледяных планетах.
    .suffix = { ent-MobPenguin.suffix }

ent-MobSnake = змея
    .desc = Хиссс! Укусы не ядовиты.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobSpiderBase = { ent-SimpleMobBase }
    .desc = { ent-SimpleMobBase.desc }
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobSpiderAngryBase = { ent-MobSpiderBase }
    .desc = { ent-MobSpiderBase.desc }
    .suffix = { ent-MobSpiderBase.suffix }

ent-MobGiantSpider = тарантул
    .desc = Общепризнанно, что это буквально худшее существо на свете.
    .suffix = { ent-MobSpiderBase.suffix }

ent-MobGiantSpiderAngry = { ent-MobGiantSpider }
    .desc = { ent-MobGiantSpider.desc }
    .suffix = { ent-MobSpiderAngryBase.suffix }

ent-MobClownSpider = клоун-паук
    .desc = Сочетает в себе две самые страшные вещи на свете — пауков и клоунов.
    .suffix = { ent-MobSpiderAngryBase.suffix }

ent-MobGiantSpiderWizard = паук-волшебник
    .desc = Этот паук выглядит немного волшебным.
    .suffix = Волшебник

ent-MobPossum = поссум
    .desc = "О поссум! Мой поссум!" -- Уолт Уитмен, 1865.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobPossumOld = поссум
    .desc = { ent-MobPossum.desc }
    .suffix = Старый спрайт

ent-MobRaccoon = енот
    .desc = Мусорная панда!
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobFox = лиса
    .desc = Они — лисы.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobCorgiBase = корги
    .desc = Наконец-то, космический корги!
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobCorgi = { ent-MobCorgiBase }
    .desc = { ent-MobCorgiBase.desc }
    .suffix = { ent-MobCorgiBase.suffix }

ent-MobCorgiNarsi = порченный корги
    .desc = Иан! Нет!
    .suffix = { ent-MobCorgi.suffix }

ent-MobCorgiPuppy = щенок корги
    .desc = Маленький корги! Оуу...
    .suffix = { ent-MobCorgi.suffix }

ent-MobCat = кошка
    .desc = Питомец семейства кошачьих, очень забавный.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobCatCalico = трёхцветная кошка
    .desc = Питомец семейства кошачьих, очень забавный.
    .suffix = { ent-MobCat.suffix }

ent-MobCatSyndy = синдикот
    .desc = Взрывоопасный котёнок.
    .suffix = { ent-MobCatSpace.suffix }

ent-MobCatSpace = космическая кошка
    .desc = Питомец семейства кошачьих, подготовленный к худшему.
    .suffix = { ent-MobCat.suffix }

ent-MobCatCaracal = каракаловая кошка
    .desc = Весёлое создание природы.
    .suffix = { ent-MobCat.suffix }

ent-MobCatKitten = котёнок
    .desc = Маленький и пушистый.
    .suffix = { ent-MobCat.suffix }

ent-MobSloth = ленивец
    .desc = Очень медлительное животное. Для людей с низкой энергией.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobFerret = хорёк
    .desc = Просто маленький глупый парнишка!
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobHamster = хомяк
    .desc = Милый, пушистый, робастный хомяк.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobPig = свинья
    .desc = Хрю.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobDionaNymph = нимфа дионы
    .desc = Похожа на кошку, только.... ветвистее.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobDionaNymphAccent = { ent-MobDionaNymph }
    .desc = { ent-MobDionaNymph.desc }
    .suffix = Акцент

ent-MobReindeerBuck = северный олень-самец
    .desc = Думаете, он может тянуть сани?
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobReindeerDoe = северный олень-самка
    .desc = { ent-MobReindeerBuck.desc }
    .suffix = { ent-MobReindeerBuck.suffix }

ent-MobArgocyteSlurva = ленивчик
    .desc = Жалкое создание, ни на что не способное.
    .suffix = { ent-BaseMobArgocyte.suffix }

ent-MobArgocyteBarrier = барьер
    .desc = { ent-BaseMobArgocyte.desc }
    .suffix = { ent-BaseMobArgocyte.suffix }

ent-MobArgocyteSkitter = суетун
    .desc = Маленький коварный пришелец... Следите за тем, чтобы он не сбежал с вашими пайками!
    .suffix = { ent-BaseMobArgocyte.suffix }

ent-MobArgocyteSwiper = тягальщик
    .desc = А куда делась эта стопка стали?
    .suffix = { ent-BaseMobArgocyte.suffix }

ent-MobArgocyteMolder = формовщик
    .desc = { ent-BaseMobArgocyte.desc }
    .suffix = { ent-BaseMobArgocyte.suffix }

ent-MobArgocytePouncer = прыгун
    .desc = { ent-BaseMobArgocyte.desc }
    .suffix = { ent-BaseMobArgocyte.suffix }

ent-MobArgocyteGlider = скользун
    .desc = { ent-BaseMobArgocyte.desc }
    .suffix = { ent-BaseMobArgocyte.suffix }

ent-MobArgocyteHarvester = сборщик
    .desc = { ent-BaseMobArgocyte.desc }
    .suffix = { ent-BaseMobArgocyte.suffix }

ent-MobArgocyteCrawler = ползун
    .desc = Смертоносные стайные животные, задирающие ни о чём не подозревающих путешественников.
    .suffix = { ent-BaseMobArgocyte.suffix }

ent-MobArgocyteEnforcer = силач
    .desc = { ent-BaseMobArgocyte.desc }
    .suffix = { ent-BaseMobArgocyte.suffix }

ent-MobArgocyteFounder = прародитель
    .desc = { ent-BaseMobArgocyte.desc }
    .suffix = { ent-BaseMobArgocyte.suffix }

ent-MobArgocyteLeviathing = левиазверь
    .desc = { ent-BaseMobArgocyte.desc }
    .suffix = { ent-BaseMobArgocyte.suffix }

ent-BaseMobAsteroid = { ent-BaseMob }
    .desc = { ent-BaseMob.desc }

ent-MobGoliath = голиаф
    .desc = Массивное чудовище, использующее свои длинные щупальца для ловли добычи. Не рекомендуется угрожать им ни при каких условиях.

ent-ActionGoliathTentacle = [color=red]Удар щупальцем[/color]
    .desc = Используйте свои щупальца, чтобы схватить и оглушить игрока!

ent-GoliathTentacle = щупальце
    .desc = { "" }

ent-BaseEffectGoliathTentacleSpawn = щупальце
    .desc = { "" }

ent-EffectGoliathTentacleSpawn = щупальце
    .desc = { ent-BaseEffectGoliathTentacleSpawn.desc }

ent-EffectGoliathTentacleRetract = { ent-BaseEffectGoliathTentacleSpawn }
    .desc = { ent-BaseEffectGoliathTentacleSpawn.desc }

ent-MobHivelord = повелитель роя
    .desc = Воистину инопланетное существо, представляющее собой массу неизвестного органического, постоянно колышущегося материала. Во время атаки его части отделяются и атакуют совместно с оригиналом.

ent-MobHivelordBrood = отпрыск повелителя роя
    .desc = Осколок оригинального повелителя роя, поддерживающий своего оригинала. Один особой угрозы не представляет, но...

ent-FoodHivelordRemains = остатки повелителя роя
    .desc = Это всё, что осталось от повелителя роя, и, похоже, именно это позволяет ему безвредно отделять от себя части... Его целебные свойства скоро утратят силу, если не воспользоваться ими поскорее. Постарайтесь не думать о том, что именно вы едите.

ent-FoodHivelordRemainsInert = инертные остатки повелителя роя
    .desc = Это всё, что осталось от повелителя роя... Теперь точно всё.

ent-MobBasilisk = василиск
    .desc = Территориальное чудовище, покрытое толстой оболочкой, поглощающей энергию. Его взгляд заставляет жертв застывать изнутри.

ent-BaseMobBehonker = бехонкер
    .desc = Парящий демонический аспект хонкоматери.
    .suffix = { ent-SimpleSpaceMobBase.suffix }

ent-MobBehonkerElectrical = бехонкер
    .desc = { ent-BaseMobBehonker.desc }
    .suffix = Электро

ent-MobBehonkerPyro = бехонкер
    .desc = { ent-BaseMobBehonker.desc }
    .suffix = Пиро

ent-MobBehonkerGrav = бехонкер
    .desc = { ent-BaseMobBehonker.desc }
    .suffix = Гравитация

ent-MobBehonkerIce = бехонкер
    .desc = { ent-BaseMobBehonker.desc }
    .suffix = Лёд

ent-BaseMobCarp = космический карп
    .desc = Это космический карп.
    .suffix = { ent-SimpleSpaceMobBase.suffix }

ent-MobCarp = { ent-BaseMobCarp }
    .desc = { ent-BaseMobCarp.desc }
    .suffix = { ent-BaseMobCarp.suffix }

ent-MobCarpMagic = мэджикарп
    .desc = Похож на какую-то рыбу. Может быть волшебным.
    .suffix = { ent-BaseMobCarp.suffix }

ent-MobCarpHolo = голокарп
    .desc = Карп из голографической энергии. К сожалению для вас, он вполне реален.
    .suffix = { ent-BaseMobCarp.suffix }

ent-MobCarpRainbow = радужный карп
    .desc = Ух ты, какая блестящая рыбка!
    .suffix = { ent-MobCarp.suffix }

ent-MobCarpSalvage = { ent-MobCarp }
    .desc = { ent-MobCarp.desc }
    .suffix = Подземелье

ent-MobCarpDragon = космический карп
    .desc = { ent-MobCarp.desc }
    .suffix = ВыводокДракона

ent-MobCarpDungeon = { ent-MobCarp }
    .desc = { ent-MobCarp.desc }
    .suffix = Подземелье

ent-MobShark = карпоакула
    .desc = Опасная акула из черноты бесконечного космоса, которая любит пить кровь.
    .suffix = { ent-BaseMobCarp.suffix }

ent-MobSharkSalvage = { ent-MobShark }
    .desc = { ent-MobShark.desc }
    .suffix = Подземелье

ent-MobElementalBase = { ent-BaseMob }
    .desc = { ent-BaseMob.desc }

ent-MobOreCrab = рудокраб
    .desc = { ent-MobElementalBase.desc }

ent-MobQuartzCrab = { ent-MobOreCrab }
    .desc = Рудный краб, состоящий из кварца.
    .suffix = Кварц

ent-MobIronCrab = { ent-MobOreCrab }
    .desc = Рудный краб, состоящий из железа.
    .suffix = Железо

ent-MobCoalCrab = { ent-MobOreCrab }
    .desc = Рудный краб, состоящий из угля.
    .suffix = Уголь

ent-MobUraniumCrab = { ent-MobOreCrab }
    .desc = Рудный краб, состоящий из урана.
    .suffix = Уран

ent-MobBananiumCrab = { ent-MobOreCrab }
    .desc = Рудный краб, состоящий из бананиума.
    .suffix = Бананиум

ent-MobSilverCrab = { ent-MobOreCrab }
    .desc = Рудный краб, состоящий из серебра.
    .suffix = Серебро

ent-MobGoldCrab = { ent-MobOreCrab }
    .desc = Рудный краб, состоящий из золота.
    .suffix = Золото

ent-ReagentSlime = химический слайм
    .desc = Состоит из жидкости и хочет растворить вас в себе.
    .suffix = Вода

ent-ReagentSlimeSpawner = спавнер химический слайм
    .desc = { ent-MarkerBase.desc }

ent-ReagentSlimeBeer = { ent-ReagentSlime }
    .desc = { ent-ReagentSlime.desc }
    .suffix = Пиво

ent-ReagentSlimePax = { ent-ReagentSlime }
    .desc = { ent-ReagentSlime.desc }
    .suffix = Пакс

ent-ReagentSlimeNocturine = { ent-ReagentSlime }
    .desc = { ent-ReagentSlime.desc }
    .suffix = Ноктюрин

ent-ReagentSlimeTHC = { ent-ReagentSlime }
    .desc = { ent-ReagentSlime.desc }
    .suffix = ТГК

ent-ReagentSlimeBicaridine = { ent-ReagentSlime }
    .desc = { ent-ReagentSlime.desc }
    .suffix = Бикаридин

ent-ReagentSlimeToxin = { ent-ReagentSlime }
    .desc = { ent-ReagentSlime.desc }
    .suffix = Токсин

ent-ReagentSlimeNapalm = { ent-ReagentSlime }
    .desc = { ent-ReagentSlime.desc }
    .suffix = Напалм

ent-ReagentSlimeOmnizine = { ent-ReagentSlime }
    .desc = { ent-ReagentSlime.desc }
    .suffix = Омнизин

ent-ReagentSlimeMuteToxin = { ent-ReagentSlime }
    .desc = { ent-ReagentSlime.desc }
    .suffix = Токсин немоты

ent-ReagentSlimeNorepinephricAcid = { ent-ReagentSlime }
    .desc = { ent-ReagentSlime.desc }
    .suffix = Норэпинефриновая кислота

ent-ReagentSlimeEphedrine = { ent-ReagentSlime }
    .desc = { ent-ReagentSlime.desc }
    .suffix = Эфедрин

ent-ReagentSlimeRobustHarvest = { ent-ReagentSlime }
    .desc = { ent-ReagentSlime.desc }
    .suffix = Робаст харвест

ent-BaseMobFlesh = искажённая плоть
    .desc = Колышущаяся масса плоти, оживлённая под действием аномальной энергии.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobFleshJared = { ent-BaseMobFlesh }
    .desc = { ent-BaseMobFlesh.desc }
    .suffix = { ent-BaseMobFlesh.suffix }

ent-MobFleshGolem = { ent-BaseMobFlesh }
    .desc = { ent-BaseMobFlesh.desc }
    .suffix = { ent-BaseMobFlesh.suffix }

ent-MobFleshClamp = { ent-BaseMobFlesh }
    .desc = { ent-BaseMobFlesh.desc }
    .suffix = { ent-BaseMobFlesh.suffix }

ent-MobFleshLover = { ent-BaseMobFlesh }
    .desc = { ent-BaseMobFlesh.desc }
    .suffix = { ent-BaseMobFlesh.suffix }

ent-MobAbomination = мерзость
    .desc = Бракованный клон, испытывающий постоянную боль и жаждущий мести.
    .suffix = { ent-BaseMobFlesh.suffix }

ent-BaseMobFleshSalvage = искажённая плоть
    .desc = Колышущаяся масса плоти, оживлённая под действием аномальной энергии.
    .suffix = Обломок

ent-MobFleshJaredSalvage = { ent-BaseMobFleshSalvage }
    .desc = { ent-BaseMobFleshSalvage.desc }
    .suffix = { ent-BaseMobFleshSalvage.suffix }

ent-MobFleshGolemSalvage = { ent-BaseMobFleshSalvage }
    .desc = { ent-BaseMobFleshSalvage.desc }
    .suffix = { ent-BaseMobFleshSalvage.suffix }

ent-MobFleshClampSalvage = { ent-BaseMobFleshSalvage }
    .desc = { ent-BaseMobFleshSalvage.desc }
    .suffix = { ent-BaseMobFleshSalvage.suffix }

ent-MobFleshLoverSalvage = { ent-BaseMobFleshSalvage }
    .desc = { ent-BaseMobFleshSalvage.desc }
    .suffix = { ent-BaseMobFleshSalvage.suffix }

ent-FlyingMobBase = { "" }
    .desc = { "" }

ent-MobHellspawn = адское отродье
    .desc = Неудержимая сила резни.
    .suffix = { ent-BaseSimpleMob.suffix }

ent-MobHivebot = ройбот
    .desc = Раздражающие, механизированные вредители.
    .suffix = { ent-BaseSimpleMob.suffix }

ent-MobHivebotRanged = ройбот
    .desc = Раздражающие, механизированные вредители. У этого есть оружие.
    .suffix = Дальний бой

ent-MobHivebotStrong = сильный ройбот
    .desc = Раздражающие, механизированные вредители. Этот выглядит сильнее обычного.
    .suffix = { ent-MobHivebotRanged.suffix }

ent-MobCivilian = гражданский
    .desc = Жалкая кучка тайн.

ent-MobSalvager = утилизатор
    .desc = { ent-MobHuman.desc }

ent-MobSpirate = космопират
    .desc = Яррр!

ent-MobSyndicateFootsoldier = пехотинец Синдиката
    .desc = { ent-MobHuman.desc }

ent-MobSyndicateFootsoldierPilot = пилот шаттла Синдиката
    .desc = { ent-MobSyndicateFootsoldier.desc }

ent-SalvageHumanCorpse = неопознанный труп
    .desc = Я думаю, оно мертво.
    .suffix = Мёртвый

ent-MobCluwne = существо
    .desc = Полиморфированное несчастье.

ent-MobWatcherLavaland = { ent-MobWatcherBase }
    .desc = { ent-MobWatcherBase.desc }
    .suffix = { ent-MobWatcherBase.suffix }

ent-MobWatcherIcewing = наблюдатель-ледокрыл
    .desc = { ent-MobWatcherBase.desc }
    .suffix = { ent-MobWatcherBase.suffix }

ent-MobWatcherMagmawing = наблюдатель-магмакрыл
    .desc = { ent-MobWatcherBase.desc }
    .suffix = { ent-MobWatcherBase.suffix }

ent-MobWatcherPride = гордый наблюдатель
    .desc = Этот редкий подвид появляется только в июне.
    .suffix = Адмемы

ent-MobLivingLight = светящаяся персона
    .desc = Ослепительная фигура из чистого света, кажущаяся неосязаемой.

ent-MobLuminousPerson = { ent-MobLivingLight }
    .desc = { ent-MobLivingLight.desc }

ent-MobLuminousObject = светящийся объект
    .desc = Небольшой светящийся объект, своим свечением обжигающий кожу.

ent-MobLuminousEntity = светящаяся сущность
    .desc = Ослепительная прозрачная сущность, чей яркий глаз кажется опасным и обжигающим.

ent-MobLuminousPersonSalvage = { ent-MobLuminousPerson }
    .desc = { ent-MobLuminousPerson.desc }
    .suffix = Salvage Ruleset

ent-MobLuminousObjectSalvage = { ent-MobLuminousObject }
    .desc = { ent-MobLuminousObject.desc }
    .suffix = Salvage Ruleset

ent-MobLuminousEntitySalvage = { ent-MobLuminousEntity }
    .desc = { ent-MobLuminousEntity.desc }
    .suffix = Salvage Ruleset

ent-MobMimic = Мимик
    .desc = Сюрприз.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobLaserRaptor = лазерный раптор
    .desc = Из эпохи викингов.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobTomatoKiller = помидор-убийца
    .desc = Похоже, сегодня не вы едите помидоры, а помидоры едят вас.
    .suffix = { ent-BaseSimpleMob.suffix }

ent-MobMoproach = швабракан
    .desc = У этого маленького швабракана тапочки-швабры на лапках! Как же очаровательно!
    .suffix = { ent-MobMothroach.suffix }

ent-MobCorgiIan = Иан
    .desc = Любимое домашнее животное — корги.
    .suffix = { ent-MobCorgi.suffix }

ent-MobCorgiIanOld = Старый Иан
    .desc = Всё ещё любимый домашний корги. Любит свои колёса.
    .suffix = { ent-MobCorgiIan.suffix }

ent-MobCorgiLisa = Лиза
    .desc = Любимая корги Иана.
    .suffix = { ent-MobCorgiIan.suffix }

ent-MobCorgiMouse = настоящая мышь
    .desc = Это на 100% настоящая голодная мышь.
    .suffix = { ent-MobCorgiIan.suffix }

ent-MobCorgiIanPup = щенок Иан
    .desc = Любимый щенок корги. Аввв.
    .suffix = { ent-MobCorgiPuppy.suffix }

ent-MobCatRuntime = Рантайм
    .desc = Профессиональный охотник на мышей. Мастер побега.
    .suffix = { ent-MobCat.suffix }

ent-MobCatException = Эксепшен
    .desc = Хорошенько попросите, и, возможно, они дадут вам одну из своих запасных жизней.
    .suffix = { ent-MobCatCalico.suffix }

ent-MobCatFloppa = Шлёпа
    .desc = Он здесь.
    .suffix = { ent-MobCatCaracal.suffix }

ent-MobBandito = Бандито
    .desc = Просто маленький глупый парнишка!
    .suffix = { ent-MobFerret.suffix }

ent-MobBingus = бингус
    .desc = Бингус, мой любимый...
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobMcGriff = МакГрифф
    .desc = Этот пёс может сказать, что здесь чем-то попахивает, и это что-то — ПРЕСТУПЛЕНИЕ!
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobPaperwork = Пэйперворк
    .desc = Устроился на новую работу по сортировке книг в библиотеке после того, как его перевели с Космической Станции 13. Он, похоже, очень медленно справляется с этой работой.
    .suffix = { ent-MobSloth.suffix }

ent-MobWalter = Уолтер
    .desc = Он обожает химию и угощения. Уолтер.
    .suffix = { ent-SimpleMobBase.suffix }

ent-MobPossumMorty = Морти
    .desc = Обитатель станции, Виргинский опоссум. Чувствительный, но стойкий парень.
    .suffix = { ent-MobPossum.suffix }

ent-MobPossumMortyOld = Морти
    .desc = { ent-MobPossumMorty.desc }
    .suffix = Старый спрайт

ent-MobPossumPoppy = Поппи
    .desc = Это опоссум, небольшое сумчатое животное, питающееся отбросами. На него надеты соответствующие средства индивидуальной защиты.
    .suffix = { ent-MobPossumMorty.suffix }

ent-MobRaccoonMorticia = Мортиша
    .desc = Могущественное создание ночи. Её тени для век всегда на высоте.
    .suffix = { ent-MobRaccoon.suffix }

ent-MobAlexander = Александр
    .desc = Лучший коллега повара.
    .suffix = { ent-MobPig.suffix }

ent-MobFoxRenault = Алиса
    .desc = Верная лиса капитана.
    .suffix = { ent-MobFox.suffix }

ent-MobHamsterHamlet = Гамлет
    .desc = Ворчливый, милый и пушистый хомяк.
    .suffix = { ent-MobHamster.suffix }

ent-MobHamsterHamletSlippery = { ent-MobHamsterHamlet }
    .desc = { ent-MobHamsterHamlet.desc }
    .suffix = Скользкий
