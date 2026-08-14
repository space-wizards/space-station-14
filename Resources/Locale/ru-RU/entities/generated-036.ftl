ent-BaseSubstationWall = настенная подстанция
    .desc = Подстанция, предназначенная для компактных шаттлов и помещений.

ent-SubstationBasic = { ent-BaseSubstation }
    .desc = { ent-BaseSubstation.desc }
    .suffix = Базовая, 2,5МДж

ent-SubstationBasicEmpty = { ent-SubstationBasic }
    .desc = { ent-SubstationBasic.desc }
    .suffix = Пустой

ent-SubstationWallBasic = { ent-BaseSubstationWall }
    .desc = { ent-BaseSubstationWall.desc }
    .suffix = Базовая, 2МДж

ent-BaseSubstationWallFrame = каркас настенной подстанции
    .desc = Каркас для строительства подстанции.

ent-ShuttleGunBase = пушка щиттла
    .desc = { ent-BaseStructure.desc }

ent-ShuttleGunSvalinnMachineGun = LSE-400c "Пулемёт Свалинн"
    .desc = Базовая стационарная лазерная установка. Эффективна против живой силы и электроприборов. Для стрельбы использует обычые батареи и обладает чрезвычайно высокой скорострельностью.

ent-ShuttleGunPerforator = LSE-1200c "Перфоратор"
    .desc = Продвинутая стационарная лазерная установка. Уничтожает электроприборы и чрезвычайно опасна для здоровья! Для стрельбы использует энергоячейки.

ent-ShuttleGunFriendship = EXP-320g "Дружба"
    .desc = Небольшой стационарный гранатомёт, вмещающий 2 гранаты.

ent-ShuttleGunDuster = EXP-2100g "Дастер"
    .desc = Мощный стационарный гранатомёт. Для стрельбы необходим картридж.

ent-ShuttleGunPirateCannon = пушка пиратского корабля
    .desc = Кабум!

ent-ShuttleGunKinetic = PTK-800 "Дематериализатор материи"
    .desc = Стационарная добывающая турель утилизаторов. Самостоятельно накапливает заряды, чрезвычайно эффективна при раскопке астероидов.

ent-ShuttleGunKineticOld = экзоморфный дематериализатор
    .desc = Древнее корабельное орудие, использующее биомеханические системы для хранения и высвобождения энергию. Уникальное в своём роде. Несмотря на уникальность конструкции, оно устарело из-за низкой скорости зарядки и стрельбы по сравнению с механическими конструкциями.

ent-StationAnchorIndestructible = { ent-StationAnchorBase }
    .desc = { ent-StationAnchorBase.desc }
    .suffix = Неразрушимый, Не требует питания

ent-StationAnchor = { ent-StationAnchorBase }
    .desc = { ent-StationAnchorBase.desc }
    .suffix = { ent-StationAnchorBase.suffix }

ent-StationAnchorOff = { ent-StationAnchor }
    .desc = { ent-StationAnchor.desc }
    .suffix = Выключен

ent-BaseThruster = ракетный двигатель
    .desc = Ускоритель, позволяющий шаттлу передвигаться.

ent-Thruster = ракетный двигатель
    .desc = { ent-BaseThruster.desc }

ent-ThrusterXenoborg = { ent-Thruster }
    .desc = { ent-Thruster.desc }

ent-ThrusterLarge = большой ракетный двигатель
    .desc = { ent-BaseThruster.desc }

ent-ThrusterUnanchored = { ent-Thruster }
    .desc = { ent-Thruster.desc }
    .suffix = Незакреплённый

ent-DebugThruster = { ent-BaseThruster }
    .desc = { ent-BaseThruster.desc }
    .suffix = DEBUG

ent-Gyroscope = гироскоп
    .desc = Увеличивает потенциальное угловое вращение шаттла.

ent-GyroscopeUnanchored = { ent-Gyroscope }
    .desc = { ent-Gyroscope.desc }
    .suffix = Незакреплённый

ent-DebugGyroscope = { ent-BaseThruster }
    .desc = { ent-BaseThruster.desc }
    .suffix = DEBUG

ent-RustedThruster = ржавый ракетный двигатель
    .desc = Неподлежащий ремонту двигатель, вышел из строя из-за износа. Пригоден только на металлолом.

ent-hydroponicsSoil = почва
    .desc = A mix of organic matter and minerals creating a soil to grow your plant in space. Seems to be dry.

ent-FungalSoil = грибная почва
    .desc = Смесь органических веществ и корней грибов, создающая почву для выращивания растений в космосе. Кажется сухой.

ent-BaseAnomaly = аномалия
    .desc = Непостижимый объект в пространстве. Стоит ли стоять так близко к нему?

ent-AnomalyPyroclastic = { ent-BaseAnomaly }
    .desc = { ent-BaseAnomaly.desc }
    .suffix = Пирокластика

ent-AnomalyGravity = { ent-BaseAnomaly }
    .desc = { ent-BaseAnomaly.desc }
    .suffix = Гравитация

ent-AnomalyElectricity = { ent-BaseAnomaly }
    .desc = { ent-BaseAnomaly.desc }
    .suffix = Электричество

ent-AnomalyFlesh = { ent-BaseAnomaly }
    .desc = { ent-BaseAnomaly.desc }
    .suffix = Плоть

ent-AnomalyBluespace = { ent-BaseAnomaly }
    .desc = { ent-BaseAnomaly.desc }
    .suffix = Блюспейс

ent-AnomalyIce = { ent-BaseAnomaly }
    .desc = { ent-BaseAnomaly.desc }
    .suffix = Лёд

ent-AnomalyRockBase = { ent-BaseAnomaly }
    .desc = { ent-BaseAnomaly.desc }
    .suffix = Камень

ent-AnomalyRockUranium = { ent-AnomalyRockBase }
    .desc = { ent-AnomalyRockBase.desc }
    .suffix = Камень, Уран

ent-AnomalyRockBananium = { ent-AnomalyRockBase }
    .desc = { ent-AnomalyRockBase.desc }
    .suffix = Камень, Бананиум

ent-AnomalyRockQuartz = { ent-AnomalyRockBase }
    .desc = { ent-AnomalyRockBase.desc }
    .suffix = Камень, Кварц

ent-AnomalyRockSilver = { ent-AnomalyRockBase }
    .desc = { ent-AnomalyRockBase.desc }
    .suffix = Камень, Серебро

ent-AnomalyRockGold = { ent-AnomalyRockBase }
    .desc = { ent-AnomalyRockBase.desc }
    .suffix = Камень, Золото

ent-AnomalyRockIron = { ent-AnomalyRockBase }
    .desc = { ent-AnomalyRockBase.desc }
    .suffix = Камень, Железо

ent-AnomalyRockCoal = { ent-AnomalyRockBase }
    .desc = { ent-AnomalyRockBase.desc }
    .suffix = Камень, Уголь

ent-AnomalyFlora = { ent-BaseAnomaly }
    .desc = { ent-BaseAnomaly.desc }
    .suffix = Флора

ent-AnomalyFloraBulb = странная светящаяся ягода
    .desc = Это красивая странно светящаяся ягода. Кажется, что внутри неё что-то растёт...
    .suffix = Аномалия Флора

ent-AnomalyLiquid = { ent-BaseAnomaly }
    .desc = { ent-BaseAnomaly.desc }
    .suffix = Жидкость

ent-AnomalyShadow = { ent-BaseAnomaly }
    .desc = { ent-BaseAnomaly.desc }
    .suffix = Тень

ent-AnomalyTech = { ent-BaseAnomaly }
    .desc = { ent-BaseAnomaly.desc }
    .suffix = Тех

ent-AnomalyTechBeam = { "" }
    .desc = { "" }

ent-AnomalySanta = { ent-BaseAnomaly }
    .desc = { ent-BaseAnomaly.desc }
    .suffix = Санта

ent-AnomalyInjectionBase = { "" }
    .desc = { "" }

ent-AnomalyInjectionPyroclastic = { ent-AnomalyInjectionBase }
    .desc = { ent-AnomalyInjectionBase.desc }

ent-AnomalyInjectionElectric = { ent-AnomalyInjectionBase }
    .desc = { ent-AnomalyInjectionBase.desc }

ent-AnomalyInjectionShadow = { ent-AnomalyInjectionBase }
    .desc = { ent-AnomalyInjectionBase.desc }

ent-AnomalyInjectionIce = { ent-AnomalyInjectionBase }
    .desc = { ent-AnomalyInjectionBase.desc }

ent-AnomalyInjectionFlora = { ent-AnomalyInjectionBase }
    .desc = { ent-AnomalyInjectionBase.desc }

ent-AnomalyInjectionBluespace = { ent-AnomalyInjectionBase }
    .desc = { ent-AnomalyInjectionBase.desc }

ent-AnomalyInjectionFlesh = { ent-AnomalyInjectionBase }
    .desc = { ent-AnomalyInjectionBase.desc }

ent-AnomalyInjectionGravity = { ent-AnomalyInjectionBase }
    .desc = { ent-AnomalyInjectionBase.desc }

ent-AnomalyInjectionTech = { ent-AnomalyInjectionBase }
    .desc = { ent-AnomalyInjectionBase.desc }

ent-AnomalyInjectionRock = { ent-AnomalyInjectionBase }
    .desc = { ent-AnomalyInjectionBase.desc }

ent-AnomalyInjectionSanta = { ent-AnomalyInjectionBase }
    .desc = { ent-AnomalyInjectionBase.desc }

ent-BaseAnomalyInjector = аномалия-инъектор
    .desc = { ent-MarkerBase.desc }

ent-AnomalyTrapPyroclastic = { ent-BaseAnomalyInjector }
    .desc = { ent-BaseAnomalyInjector.desc }
    .suffix = Пирокластика

ent-AnomalyTrapElectricity = { ent-BaseAnomalyInjector }
    .desc = { ent-BaseAnomalyInjector.desc }
    .suffix = Электричество

ent-AnomalyTrapShadow = { ent-BaseAnomalyInjector }
    .desc = { ent-BaseAnomalyInjector.desc }
    .suffix = Тень

ent-AnomalyTrapIce = { ent-BaseAnomalyInjector }
    .desc = { ent-BaseAnomalyInjector.desc }
    .suffix = Лёд

ent-AnomalyTrapFlora = { ent-BaseAnomalyInjector }
    .desc = { ent-BaseAnomalyInjector.desc }
    .suffix = Флора

ent-AnomalyTrapBluespace = { ent-BaseAnomalyInjector }
    .desc = { ent-BaseAnomalyInjector.desc }
    .suffix = Блюспейс

ent-AnomalyTrapFlesh = { ent-BaseAnomalyInjector }
    .desc = { ent-BaseAnomalyInjector.desc }
    .suffix = Плоть

ent-AnomalyTrapGravity = { ent-BaseAnomalyInjector }
    .desc = { ent-BaseAnomalyInjector.desc }
    .suffix = Гравитация

ent-AnomalyTrapTech = { ent-BaseAnomalyInjector }
    .desc = { ent-BaseAnomalyInjector.desc }
    .suffix = Тех

ent-AnomalyTrapRock = { ent-BaseAnomalyInjector }
    .desc = { ent-BaseAnomalyInjector.desc }
    .suffix = Камень

ent-AnomalyTrapSanta = { ent-BaseAnomalyInjector }
    .desc = { ent-BaseAnomalyInjector.desc }
    .suffix = Санта

ent-BaseAnomalyCore = ядро аномалии
    .desc = Ядро уничтоженного непостижимого объекта.

ent-AnomalyCorePyroclastic = { ent-BaseAnomalyCore }
    .desc = { ent-BaseAnomalyCore.desc }
    .suffix = Пирокластика

ent-AnomalyCoreGravity = { ent-BaseAnomalyCore }
    .desc = { ent-BaseAnomalyCore.desc }
    .suffix = Гравитация

ent-AnomalyCoreIce = { ent-BaseAnomalyCore }
    .desc = { ent-BaseAnomalyCore.desc }
    .suffix = Лёд

ent-AnomalyCoreFlesh = { ent-BaseAnomalyCore }
    .desc = Ядро разрушенной аномалии плоти. Тошнотворно пульсирует, но может стать сытным блюдом, если его приготовить.
    .suffix = Плоть

ent-AnomalyCoreRock = { ent-BaseAnomalyCore }
    .desc = { ent-BaseAnomalyCore.desc }
    .suffix = Камень

ent-AnomalyCoreLiquid = { ent-BaseAnomalyCore }
    .desc = { ent-BaseAnomalyCore.desc }
    .suffix = Жидкость

ent-AnomalyCoreBluespace = { ent-BaseAnomalyCore }
    .desc = { ent-BaseAnomalyCore.desc }
    .suffix = Блюспейс

ent-AnomalyCoreElectricity = { ent-BaseAnomalyCore }
    .desc = { ent-BaseAnomalyCore.desc }
    .suffix = Электричество

ent-AnomalyCoreFlora = { ent-BaseAnomalyCore }
    .desc = { ent-BaseAnomalyCore.desc }
    .suffix = Флора

ent-AnomalyCoreShadow = { ent-BaseAnomalyCore }
    .desc = { ent-BaseAnomalyCore.desc }
    .suffix = Тень

ent-AnomalyCoreTech = { ent-BaseAnomalyCore }
    .desc = { ent-BaseAnomalyCore.desc }
    .suffix = Тех

ent-AnomalyCoreSanta = { ent-BaseAnomalyCore }
    .desc = { ent-BaseAnomalyCore.desc }
    .suffix = Санта

ent-BaseAnomalyInertCore = { ent-BaseAnomalyCore }
    .desc = { ent-BaseAnomalyCore.desc }

ent-AnomalyCorePyroclasticInert = { ent-BaseAnomalyInertCore }
    .desc = { ent-BaseAnomalyInertCore.desc }
    .suffix = Пирокластика, Инертный

ent-AnomalyCoreGravityInert = { ent-BaseAnomalyInertCore }
    .desc = { ent-BaseAnomalyInertCore.desc }
    .suffix = Гравитация, Инертный

ent-AnomalyCoreIceInert = { ent-BaseAnomalyInertCore }
    .desc = { ent-BaseAnomalyInertCore.desc }
    .suffix = Лёд, Инертный

ent-AnomalyCoreFleshInert = { ent-BaseAnomalyInertCore }
    .desc = Инертное ядро разрушенной аномалии плоти. Тошнотворно пульсирует, но, возможно, в умелых руках станет сытным блюдом?
    .suffix = Плоть, Инертный

ent-AnomalyCoreRockInert = { ent-BaseAnomalyInertCore }
    .desc = { ent-BaseAnomalyInertCore.desc }
    .suffix = Камень, Инертный

ent-AnomalyCoreLiquidInert = { ent-BaseAnomalyInertCore }
    .desc = { ent-BaseAnomalyInertCore.desc }
    .suffix = Жидкость, Инертный

ent-AnomalyCoreBluespaceInert = { ent-BaseAnomalyInertCore }
    .desc = { ent-BaseAnomalyInertCore.desc }
    .suffix = Блюспейс, Инертный

ent-AnomalyCoreElectricityInert = { ent-BaseAnomalyInertCore }
    .desc = { ent-BaseAnomalyInertCore.desc }
    .suffix = Электричество, Инертный

ent-AnomalyCoreFloraInert = { ent-BaseAnomalyInertCore }
    .desc = { ent-BaseAnomalyInertCore.desc }
    .suffix = Флора, Инертный

ent-AnomalyCoreShadowInert = { ent-BaseAnomalyInertCore }
    .desc = { ent-BaseAnomalyInertCore.desc }
    .suffix = Тень, Инертный

ent-AnomalyCoreTechInert = { ent-BaseAnomalyInertCore }
    .desc = { ent-BaseAnomalyInertCore.desc }
    .suffix = Тех, Инертный

ent-AnomalyCoreSantaInert = { ent-BaseAnomalyInertCore }
    .desc = { ent-BaseAnomalyInertCore.desc }
    .suffix = Санта, Инертный

ent-AirSensorFreezer = { ent-AirSensor }
    .desc = { ent-AirSensor.desc }
    .suffix = { ent-AirSensorFreezerBase.suffix }

ent-GasVentPumpFreezer = { ent-GasVentPump }
    .desc = { ent-GasVentPump.desc }
    .suffix = { ent-AirSensorFreezerBase.suffix }

ent-GasVentScrubberFreezer = { ent-GasVentScrubber }
    .desc = { ent-GasVentScrubber.desc }
    .suffix = { ent-AirSensorFreezerBase.suffix }

ent-AirAlarmFreezer = { ent-AirAlarm }
    .desc = { ent-AirAlarm.desc }
    .suffix = Атмосфера холодильника, авто-режим отключён

ent-AirSensorBase = { "" }
    .desc = { "" }

ent-AirSensor = сенсор воздуха
    .desc = Воздушный сенсор. Чувствует воздух.

ent-AirSensorAssembly = корпус сенсора воздуха
    .desc = Корпус воздушного сенсора. Ещё не чувствует воздух.

ent-AirSensorVoxBase = { ent-AirSensorBase }
    .desc = { ent-AirSensorBase.desc }
    .suffix = Атмосфера воксов

ent-AirSensorVox = { ent-AirSensor }
    .desc = { ent-AirSensor.desc }
    .suffix = { ent-AirSensorVoxBase.suffix }

ent-GasVentPumpVox = { ent-GasVentPump }
    .desc = { ent-GasVentPump.desc }
    .suffix = { ent-AirSensorVoxBase.suffix }

ent-GasVentScrubberVox = { ent-GasVentScrubber }
    .desc = { ent-GasVentScrubber.desc }
    .suffix = { ent-AirSensorVoxBase.suffix }

ent-AirAlarmVox = { ent-AirAlarm }
    .desc = { ent-AirAlarm.desc }
    .suffix = Атмосфера воксов, авторежим выключен

ent-MailCart = почтовая тележка
    .desc = Доставляйте посылки стильно и эффективно.

ent-ChurchBell = церковный колокол
    .desc = Вы чувствуете, как с каждым ударом этого колокола, ваша душа приближается к потустороннему миру...

ent-CarpRift = карповый разлом
    .desc = Разлом, подобный тем, которые космические карпы используют для перемещения на большие расстояния.

ent-FloorDrain = дренаж
    .desc = Сливает лужи вокруг в себя. Пригодится для опорожнения вёдер или поддержания чистоты в определённых помещениях.

ent-MopBucket = ведро для швабры
    .desc = Содержит воду и слёзы уборщика.

ent-MopBucketFull = ведро для швабры
    .desc = { ent-MopBucket.desc }
    .suffix = Полный

ent-MopBucketCubeWrapped = кубик ведра для швабры
    .desc = Разверните его, чтобы получить ведро для швабры.
    .suffix = { ent-BaseWrappedCube.suffix }

ent-JanitorialTrolley = тележка уборщика
    .desc = Это альфа и омега санитарии.

ent-XenoWardingTower = охранная башня ксено
    .desc = { "" }

ent-CarpStatue = статуя карпа
    .desc = Статуя одного из храбрых карпов, благодаря которому мы находимся там, где находимся. С настоящими зубами!

ent-CarpStatueEmpty = { ent-CarpStatue }
    .desc = Глыба драгоценного металла, которая вскоре превратится в блестящую статую карпа.
    .suffix = Пустой

ent-CarpStatueEyes = { ent-CarpStatue }
    .desc = Статуя одного из храбрых карпов, из-за которых мы оказались в том положении, в котором находимся сейчас. Ему нужен стоматолог...
    .suffix = Глаза

ent-SpiderWebBase = { "" }
    .desc = { "" }

ent-SpiderWeb = паутина
    .desc = Она вязкая и липкая.

ent-SpiderWebClown = клоунская паутина
    .desc = Она вязкая и скользкая.

ent-BaseFloorDecoration = { "" }
    .desc = { "" }

ent-Stairs = ступеньки
    .desc = Величайшее изобретение после гранатомётов.
    .suffix = Стальной

ent-StairStage = { ent-Stairs }
    .desc = { ent-Stairs.desc }
    .suffix = Стальной, Одна ступень

ent-StairWhite = { ent-Stairs }
    .desc = { ent-Stairs.desc }
    .suffix = Белый

ent-StairStageWhite = { ent-Stairs }
    .desc = { ent-Stairs.desc }
    .suffix = Белый, Одна ступень

ent-StairDark = { ent-Stairs }
    .desc = { ent-Stairs.desc }
    .suffix = Тёмный

ent-StairStageDark = { ent-Stairs }
    .desc = { ent-Stairs.desc }
    .suffix = Тёмный, Одна ступень

ent-StairWood = { ent-Stairs }
    .desc = { ent-Stairs.desc }
    .suffix = Деревянный

ent-StairStageWood = { ent-Stairs }
    .desc = { ent-Stairs.desc }
    .suffix = Деревянный, Одна ступень

ent-BaseBarrelChem = химическая бочка
    .desc = Маленькая металлическая бочка. Как по-тропически.

ent-BaseBarrelChemRadioactive = радиоактивная бочка
    .desc = Похоже, она протекает. Не думаю, что вы захотите долго находиться возле неё.

ent-BarrelChemEmpty = { ent-BaseBarrelChem }
    .desc = { ent-BaseBarrelChem.desc }
    .suffix = Пустой

ent-BarrelChemFilledIodine = { ent-BaseBarrelChem }
    .desc = { ent-BaseBarrelChem.desc }
    .suffix = Йод

ent-BarrelChemFilledFluorine = { ent-BaseBarrelChem }
    .desc = { ent-BaseBarrelChem.desc }
    .suffix = Фтор

ent-BarrelChemFilledChlorine = { ent-BaseBarrelChem }
    .desc = { ent-BaseBarrelChem.desc }
    .suffix = Хлор

ent-BarrelChemFilledEthanol = { ent-BaseBarrelChem }
    .desc = { ent-BaseBarrelChem.desc }
    .suffix = Этанол

ent-BarrelChemFilledPhosphorus = { ent-BaseBarrelChem }
    .desc = { ent-BaseBarrelChem.desc }
    .suffix = Фосфор

ent-BarrelChemFilledMercury = { ent-BaseBarrelChem }
    .desc = { ent-BaseBarrelChem.desc }
    .suffix = Ртуть

ent-BarrelChemFilledSilicon = { ent-BaseBarrelChem }
    .desc = { ent-BaseBarrelChem.desc }
    .suffix = Кремний

ent-BarrelChemFilledLube = { ent-BaseBarrelChem }
    .desc = Скользко...
    .suffix = Смазка

ent-BaseBarrelChemRadioactiveEmpty = { ent-BaseBarrelChemRadioactive }
    .desc = { ent-BaseBarrelChemRadioactive.desc }
    .suffix = Пустой

ent-BaseBarrelChemRadioactiveFilledRadium = { ent-BaseBarrelChemRadioactive }
    .desc = { ent-BaseBarrelChemRadioactive.desc }
    .suffix = Радий

ent-BaseBarrelChemRadioactiveFilledUranium = { ent-BaseBarrelChemRadioactive }
    .desc = { ent-BaseBarrelChemRadioactive.desc }
    .suffix = Уран

ent-BaseItemCabinet = { "" }
    .desc = { "" }

ent-BaseItemCabinetGlass = { ent-BaseItemCabinet }
    .desc = { ent-BaseItemCabinet.desc }

ent-GasCanister = канистра для газа
    .desc = Канистра, в которой может содержаться газ любого вида. Можно прикрепить к порту коннектора с помощью гаечного ключа.

ent-StorageCanister = канистра для хранения
    .desc = { ent-GasCanister.desc }

ent-AirCanister = канистра воздуха
    .desc = Канистра, в которой может содержаться газ любого вида. В этой, предположительно, содержится воздушная смесь. Можно прикрепить к порту коннектора с помощью гаечного ключа.

ent-OxygenCanister = канистра кислорода
    .desc = Канистра, в которой может содержаться газ любого вида. В этой, предположительно, содержится кислород. Можно прикрепить к порту коннектора с помощью гаечного ключа.

ent-LiquidOxygenCanister = канистра сжиженного кислорода
    .desc = Канистра, в которой может содержаться газ любого вида. В этой, предположительно, содержится сжиженный кислород. Можно прикрепить к порту коннектора с помощью гаечного ключа.

ent-NitrogenCanister = канистра азота
    .desc = Канистра, в которой может содержаться газ любого вида. В этой, предположительно, содержится азот. Можно прикрепить к порту коннектора с помощью гаечного ключа.

ent-LiquidNitrogenCanister = канистра сжиженного азота
    .desc = Канистра, в которой может содержаться газ любого вида. В этой, предположительно, содержится сжиженный азот. Можно прикрепить к порту коннектора с помощью гаечного ключа.

ent-CarbonDioxideCanister = канистра углекислого газа
    .desc = Канистра, в которой может содержаться газ любого вида. В этой, предположительно, содержится углекислый газ. Можно прикрепить к порту коннектора с помощью гаечного ключа.

ent-LiquidCarbonDioxideCanister = канистра сжиженного углекислого газа
    .desc = Канистра, в которой может содержаться газ любого вида. В этой, предположительно, содержится сжиженный углекислый газ. Можно прикрепить к порту коннектора с помощью гаечного ключа.

ent-PlasmaCanister = канистра плазмы
    .desc = Канистра, в которой может содержаться газ любого вида. В этой, предположительно, содержится плазма. Можно прикрепить к порту коннектора с помощью гаечного ключа.

ent-TritiumCanister = канистра трития
    .desc = Канистра, в которой может содержаться газ любого вида. В этой, предположительно, содержится тритий. Можно прикрепить к порту коннектора с помощью гаечного ключа.

ent-WaterVaporCanister = канистра водяного пара
    .desc = Канистра, в которой может содержаться газ любого вида. В этой, предположительно, содержится водяной пар. Можно прикрепить к порту коннектора с помощью гаечного ключа.

ent-AmmoniaCanister = канистра аммиака
    .desc = Канистра, в которой может содержаться газ любого вида. В этой, предположительно, содержится аммиак. Можно прикрепить к порту коннектора с помощью гаечного ключа.

ent-NitrousOxideCanister = канистра оксида азота
    .desc = Канистра, в которой может содержаться газ любого вида. В этой, предположительно, содержится оксид азота. Можно прикрепить к порту коннектора с помощью гаечного ключа.

ent-FrezonCanister = канистра фрезона
    .desc = Хладагент с лёгкими галлюциногенными свойствами. Развлекайтесь.

ent-MaxCapCanister = max cap in a can
    .desc = { ent-GasCanister.desc }
    .suffix = DEBUG, Max Cap

ent-GasCanisterBrokenBase = разбитая канистра для газа
    .desc = Разбитая канистра для газа. Не совсем бесполезна, так как может быть разобрана для получения высококачественных материалов.

ent-StorageCanisterBroken = { ent-GasCanisterBrokenBase }
    .desc = { ent-GasCanisterBrokenBase.desc }

ent-AirCanisterBroken = { ent-GasCanisterBrokenBase }
    .desc = { ent-GasCanisterBrokenBase.desc }

ent-OxygenCanisterBroken = { ent-GasCanisterBrokenBase }
    .desc = { ent-GasCanisterBrokenBase.desc }

ent-NitrogenCanisterBroken = { ent-GasCanisterBrokenBase }
    .desc = { ent-GasCanisterBrokenBase.desc }

ent-CarbonDioxideCanisterBroken = { ent-GasCanisterBrokenBase }
    .desc = { ent-GasCanisterBrokenBase.desc }

ent-PlasmaCanisterBroken = { ent-GasCanisterBrokenBase }
    .desc = { ent-GasCanisterBrokenBase.desc }

ent-TritiumCanisterBroken = { ent-GasCanisterBrokenBase }
    .desc = { ent-GasCanisterBrokenBase.desc }

ent-WaterVaporCanisterBroken = разбитая канистра водяного пара
    .desc = { ent-GasCanisterBrokenBase.desc }

ent-AmmoniaCanisterBroken = { ent-GasCanisterBrokenBase }
    .desc = { ent-GasCanisterBrokenBase.desc }

ent-NitrousOxideCanisterBroken = { ent-GasCanisterBrokenBase }
    .desc = { ent-GasCanisterBrokenBase.desc }

ent-FrezonCanisterBroken = { ent-GasCanisterBrokenBase }
    .desc = { ent-GasCanisterBrokenBase.desc }

ent-ClosetBase = шкаф
    .desc = Стандартное хранилище Nanotrasen.

ent-ClosetSteelBase = { ent-ClosetBase }
    .desc = { ent-ClosetBase.desc }

ent-BaseWallCloset = настенный шкаф
    .desc = Стандартное хранилище Nanotrasen, теперь и на стене.

ent-BaseWallLocker = { ent-BaseWallCloset }
    .desc = { ent-BaseWallCloset.desc }

ent-SuitStorageBase = хранилище скафандра
    .desc = Необычное высокотехнологичное хранилище, предназначенное для хранения космических скафандров.

ent-StealthBox = { ent-BaseBigBox }
    .desc = Kept ya waiting, huh?
    .suffix = Невидимость

ent-BigBox = { ent-BaseBigBox }
    .desc = { ent-BaseBigBox.desc }

ent-GhostBox = призрачная коробка
    .desc = Остерегайтесь!

ent-ClosetTool = шкаф с инструментами
    .desc = Это хранилище для инструментов.

ent-ClosetRadiationSuit = шкаф радиационных костюмов
    .desc = Это хранилище для радиационных костюмов.

ent-ClosetEmergency = аварийный шкаф
    .desc = Это хранилище для аварийных дыхательных масок и баллонов с кислородом.

ent-ClosetEmergencyN2 = аварийный азотный шкаф
    .desc = Заполнен спасательным снаряжением. При условии, что вы дышите азотом.

ent-ClosetFire = противопожарный шкаф
    .desc = Это хранилище для противопожарного снаряжения.

ent-ClosetBomb = шкаф взрывозащитного снаряжения
    .desc = Это хранилище для взрывозащитных костюмов.

ent-ClosetJanitorBomb = шкаф сапёро-уборочного костюма
    .desc = Это хранилище для уборочных взрывозащитных костюмов.

ent-ClosetL3 = шкаф снаряжения 3-го уровня биологической опасности
    .desc = Это хранилище для снаряжения 3-го уровня биологической опасности.

ent-ClosetL3Science = { ent-ClosetL3 }
    .desc = { ent-ClosetL3.desc }

ent-ClosetL3Virology = { ent-ClosetL3 }
    .desc = { ent-ClosetL3.desc }

ent-ClosetL3Security = { ent-ClosetL3 }
    .desc = { ent-ClosetL3.desc }

ent-ClosetL3Janitor = { ent-ClosetL3 }
    .desc = { ent-ClosetL3.desc }

ent-ClosetMaintenance = технический шкаф
    .desc = Это хранилище.

ent-LockerSyndicate = оружейный шкаф
    .desc = Это хранилище.

ent-ClosetBluespace = подозрительный шкаф
    .desc = Это хранилище... правда же?
    .suffix = Блюспейс

ent-ClosetBluespaceUnstable = подозрительный шкаф
    .desc = Это хранилище... правда же?
    .suffix = Блюспейс нестабильный

ent-ClosetCursed = шкаф
    .desc = Стандартное хранилище Nanotrasen.
    .suffix = проклятый

ent-LockerBase = { ent-ClosetBase }
    .desc = { ent-ClosetBase.desc }

ent-LockerBooze = шкафчик со спиртным
    .desc = Здесь бармен хранит алкоголь.

ent-LockerSteel = защищённый шкаф
    .desc = { ent-LockerBase.desc }

ent-LockerQuarterMaster = шкаф квартирмейстера
    .desc = { ent-LockerBase.desc }

ent-LockerSalvageSpecialist = снаряжение специалиста по утилизации
    .desc = Не обращайте внимания на кирку.

ent-LockerCaptain = шкаф капитана
    .desc = { ent-LockerBase.desc }

ent-LockerHeadOfPersonnel = шкаф главы персонала
    .desc = { ent-LockerBase.desc }

ent-LockerChiefEngineer = шкаф старшего инженера
    .desc = { ent-LockerBase.desc }

ent-LockerElectricalSupplies = шкаф электромонтажного оборудования
    .desc = { ent-LockerBase.desc }

ent-LockerWeldingSupplies = шкаф сварочного оборудования
    .desc = { ent-LockerBase.desc }

ent-LockerAtmospherics = шкаф атмосферного техника
    .desc = { ent-LockerBase.desc }

ent-LockerEngineer = шкаф инженера
    .desc = { ent-LockerBase.desc }

ent-LockerEvacRepair = экстренный шкаф эвакуационного шаттла
    .desc = Это сплошные чрезвычайные ситуации.

ent-LockerFreezerBase = холодильник
    .desc = { ent-LockerBase.desc }
    .suffix = Без доступа

ent-LockerFreezer = холодильник
    .desc = { ent-LockerFreezerBase.desc }
    .suffix = Кухня, Закрыт

ent-LockerBotanist = шкаф ботаника
    .desc = { ent-LockerBase.desc }

ent-LockerMedicine = шкаф для медикаментов
    .desc = Битком набит медицинскими штуками.

ent-LockerMedical = шкаф медика
    .desc = { ent-LockerBase.desc }

ent-LockerParamedic = шкаф парамедика
    .desc = { ent-LockerBase.desc }

ent-LockerChemistry = шкаф химика
    .desc = { ent-LockerBase.desc }

ent-LockerChiefMedicalOfficer = шкаф главного врача
    .desc = { ent-LockerBase.desc }

ent-LockerResearchDirector = шкаф научного руководителя
    .desc = { ent-LockerBase.desc }

ent-LockerScientist = шкаф учёного
    .desc = { ent-LockerBase.desc }

ent-LockerHeadOfSecurity = шкаф главы службы безопасности
    .desc = { ent-LockerBase.desc }

ent-LockerWarden = шкаф смотрителя
    .desc = { ent-LockerBase.desc }

ent-LockerBrigmedic = шкаф бригмедика
    .desc = { ent-LockerBase.desc }

ent-LockerSecurity = шкаф офицера службы безопасности
    .desc = { ent-LockerBase.desc }

ent-GunSafe = оружейный сейф
    .desc = { ent-LockerBase.desc }

ent-GunSafeBaseSecure = { ent-GunSafe }
    .desc = { ent-GunSafe.desc }
    .suffix = Оружейный, Закрыт

ent-GenpopBase = { "" }
    .desc = { "" }

ent-LockerPrisoner = шкаф заключённого
    .desc = Это защищённый шкафчик для персональных вещей заключённого во время его пребывания в тюрьме.
    .suffix = 1

ent-LockerPrisoner2 = { ent-LockerPrisoner }
    .desc = { ent-LockerPrisoner.desc }
    .suffix = 2

ent-LockerPrisoner3 = { ent-LockerPrisoner }
    .desc = { ent-LockerPrisoner.desc }
    .suffix = 3

ent-LockerPrisoner4 = { ent-LockerPrisoner }
    .desc = { ent-LockerPrisoner.desc }
    .suffix = 4

ent-LockerPrisoner5 = { ent-LockerPrisoner }
    .desc = { ent-LockerPrisoner.desc }
    .suffix = 5

ent-LockerPrisoner6 = { ent-LockerPrisoner }
    .desc = { ent-LockerPrisoner.desc }
    .suffix = 6

ent-LockerPrisoner7 = { ent-LockerPrisoner }
    .desc = { ent-LockerPrisoner.desc }
    .suffix = 7

ent-LockerPrisoner8 = { ent-LockerPrisoner }
    .desc = { ent-LockerPrisoner.desc }
    .suffix = 8

ent-LockerDetective = шкаф детектива
    .desc = Обычно пустой и холодный... как твоё сердце.

ent-LockerEvidence = шкаф для улик
    .desc = Для хранения пакетиков с гильзами и вещей задержанных.

ent-LockerSyndicatePersonal = оружейный шкаф
    .desc = Это персональное хранилище для оперативного снаряжения.
    .suffix = Locked

ent-LockerBluespaceStation = блюспейс шкаф
    .desc = Передовая технология шкафчикостроения.
    .suffix = Один на станцию

ent-LockerClown = шкаф клоуна
    .desc = { ent-LockerBase.desc }

ent-LockerMime = шкаф мима
    .desc = { ent-LockerBase.desc }

ent-LockerRepresentative = шкаф представителя Nanotrasen
    .desc = { ent-LockerBase.desc }
