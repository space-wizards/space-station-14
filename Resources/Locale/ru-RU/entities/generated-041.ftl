ent-BasicRoundstartVariation = { ent-BaseGameRule }
    .desc = { ent-BaseGameRule.desc }

ent-Thief = { ent-BaseGameRule }
    .desc = { ent-BaseGameRule.desc }

ent-SubWizard = { ent-BaseWizardRule }
    .desc = { ent-BaseWizardRule.desc }

ent-SubXenoborgs = { ent-BaseXenoborgsRule }
    .desc = { ent-BaseXenoborgsRule.desc }

ent-BaseUnknownShuttleRule = { ent-BaseGameRule }
    .desc = { ent-BaseGameRule.desc }

ent-BaseUnknownShuttleAnnouncedRule = { ent-BaseUnknownShuttleRule }
    .desc = { ent-BaseUnknownShuttleRule.desc }

ent-UnknownShuttleHonki = { ent-BaseUnknownShuttleAnnouncedRule }
    .desc = { ent-BaseUnknownShuttleAnnouncedRule.desc }

ent-UnknownShuttleSyndieEvacPod = { ent-BaseUnknownShuttleRule }
    .desc = { ent-BaseUnknownShuttleRule.desc }

ent-UnknownShuttleNTQuark = { ent-BaseUnknownShuttleRule }
    .desc = { ent-BaseUnknownShuttleRule.desc }

ent-UnknownShuttleInstigator = { ent-BaseUnknownShuttleRule }
    .desc = { ent-BaseUnknownShuttleRule.desc }

ent-UnknownShuttleManOWar = { ent-BaseUnknownShuttleRule }
    .desc = { ent-BaseUnknownShuttleRule.desc }

ent-BaseVariationPass = { ent-BaseGameRule }
    .desc = { ent-BaseGameRule.desc }

ent-BasicPoweredLightVariationPass = { ent-BaseVariationPass }
    .desc = { ent-BaseVariationPass.desc }

ent-SolidWallRustingVariationPass = { ent-BaseVariationPass }
    .desc = { ent-BaseVariationPass.desc }

ent-ReinforcedWallRustingVariationPass = { ent-BaseVariationPass }
    .desc = { ent-BaseVariationPass.desc }

ent-BasicTrashVariationPass = { ent-BaseVariationPass }
    .desc = { ent-BaseVariationPass.desc }

ent-BasicDecalDirtVariationPass = { ent-BaseVariationPass }
    .desc = { ent-BaseVariationPass.desc }

ent-BasicDecalGraffitiVariationPass = { ent-BaseVariationPass }
    .desc = { ent-BaseVariationPass.desc }

ent-BasicDecalBurnsVariationPass = { ent-BaseVariationPass }
    .desc = { ent-BaseVariationPass.desc }

ent-BasicDecalDirtMonospaceVariationPass = { ent-BaseVariationPass }
    .desc = { ent-BaseVariationPass.desc }

ent-BasicPuddleMessVariationPass = { ent-BaseVariationPass }
    .desc = { ent-BaseVariationPass.desc }

ent-BloodbathPuddleMessVariationPass = { ent-BaseVariationPass }
    .desc = { ent-BaseVariationPass.desc }

ent-CutWireVariationPass = { ent-BaseVariationPass }
    .desc = { ent-BaseVariationPass.desc }

ent-SolarPanelDamageVariationPass = { ent-BaseVariationPass }
    .desc = { ent-BaseVariationPass.desc }

ent-SolarPanelEmptyVariationPass = { ent-BaseVariationPass }
    .desc = { ent-BaseVariationPass.desc }

ent-InventoryBase = { "" }
    .desc = { "" }

ent-StripableInventoryBase = { ent-InventoryBase }
    .desc = { ent-InventoryBase.desc }

ent-LoadoutDummyCandles = три свечи
    .desc = Набор из трёх разноцветных свечей для тайных ритуалов!

ent-ActionAnimateSpell = Оживление
    .desc = Оживите неодушевлённый предмет!

ent-ActionSummonGhosts = Призыв призраков
    .desc = Делает всех существующих призраков видимыми навсегда.

ent-ActionSummonGuns = Призыв оружия
    .desc = АК-47 для всех! Размещает перед каждым случайное огнестрельное оружие.

ent-ActionSummonMagic = Призыв магии
    .desc = Размещает перед каждым случайный магический предмет. Что может пойти не так?

ent-CollideRune = руна столкновения
    .desc = { ent-BaseRune.desc }

ent-ActivateRune = руна активации
    .desc = { ent-CollideRune.desc }

ent-CollideTimerRune = отложенная руна столкновения
    .desc = { ent-CollideRune.desc }

ent-ExplosionRune = руна взрыва
    .desc = { ent-CollideRune.desc }

ent-StunRune = руна оглушения
    .desc = { ent-CollideRune.desc }

ent-IgniteRune = руна воспламенения
    .desc = { ent-CollideRune.desc }

ent-ExplosionTimedRune = отложенная руна взрыва
    .desc = { ent-CollideTimerRune.desc }

ent-ExplosionActivateRune = руна активации взрыва
    .desc = { ent-ActivateRune.desc }

ent-FlashRune = руна вспышки
    .desc = { ent-ActivateRune.desc }

ent-FlashRuneTimer = отложенная руна вспышки
    .desc = { ent-CollideTimerRune.desc }

ent-ActionForceWall = Магический барьер
    .desc = Создаёт магический барьер.

ent-ActionKnock = Стук
    .desc = Это заклинание открывает ближайшие двери.

ent-ActionMindSwap = Перенос разума
    .desc = Обменяйтесь телами с другим человеком!

ent-ActionFireball = Огненный шар
    .desc = Выпускает взрывающийся огненный шар в выбранном направлении.

ent-ActionFireballII = Огненный шар II
    .desc = Выпускает быстрый огненный шар.

ent-ActionFireballIII = Огненный шар III
    .desc = Самый быстрый огненный шар на Космическом Западе!

ent-ActionItemRecall = Пометить предмет
    .desc = Пометьте удерживаемый предмет, чтобы позже призывать его себе в руку.

ent-ActionRepulse = Импульс
    .desc = Отталкивает существ от заклинателя.

ent-BaseRuneAction = { ent-BaseAction }
    .desc = { ent-BaseAction.desc }

ent-ActionFlashRune = Руна вспышки
    .desc = Вызывает руну, которая ослепляет при использовании.

ent-ActionExplosionRune = Руна взрыва
    .desc = Вызывает руну, которая взрывается при использовании.

ent-ActionIgniteRune = Руна поджога
    .desc = Вызывает руну, которая поджигает при использовании.

ent-ActionStunRune = Руна оглушения
    .desc = Вызывает руну, которая оглушает при использовании.

ent-ActionSmoke = Дым
    .desc = Создаёт дым вокруг заклинателя.

ent-ActionSpawnMagicarpSpell = Призвать мэджикарпа
    .desc = Это заклинание призывает трёх мэджикарпов вам на помощь! Могут напасть на хозяина, а могут и нет.

ent-RGBStaff = RGB посох
    .desc = Помогает исправить нехватку RGB подсветки на станции.

ent-AnimationStaff = посох оживления
    .desc = Оживите неодушевлённый предмет!

ent-ActionRgbLight = { ent-BaseAction }
    .desc = { ent-BaseAction.desc }

ent-ActionBlink = Прыжок
    .desc = Телепортирует в выбранное место.

ent-ActionVoidApplause = Хлопок пустоты
    .desc = Хлопните в ладоши и поменяйтесь местами с целью.

ent-BaseEntitySpellAction = { ent-BaseAction }
    .desc = { ent-BaseAction.desc }

ent-BaseSmiteAction = { ent-BaseEntitySpellAction }
    .desc = { ent-BaseEntitySpellAction.desc }

ent-ActionSmite = Кара
    .desc = Мгновенно поражает цель.

ent-ActionSmiteNoReq = { ent-ActionSmite }
    .desc = { ent-ActionSmite.desc }

ent-ActionCluwne = Проклятие клувня
    .desc = Превратите кого-нибудь в клувня!

ent-ActionSlippery = Скользкая дорожка
    .desc = Сделайте кого-нибудь скользким!

ent-ActionChargeSpell = Зарядка
    .desc = Добавляет заряд вашей палочке.

ent-MobPathfindDummy = Pathfind манекен
    .desc = { ent-MobXenoRouny.desc }
    .suffix = ИИ

ent-BaseObjective = { "" }
    .desc = { "" }

ent-BaseLivingObjective = { ent-BaseObjective }
    .desc = { ent-BaseObjective.desc }

ent-BaseTargetObjective = { ent-BaseObjective }
    .desc = { ent-BaseObjective.desc }

ent-BaseKillObjective = { ent-BaseTargetObjective }
    .desc = { ent-BaseTargetObjective.desc }

ent-BaseSocialObjective = { ent-BaseTargetObjective }
    .desc = { ent-BaseTargetObjective.desc }

ent-BaseKeepAliveObjective = { ent-BaseSocialObjective }
    .desc = { ent-BaseSocialObjective.desc }

ent-BaseHelpProgressObjective = { ent-BaseSocialObjective }
    .desc = { ent-BaseSocialObjective.desc }

ent-BaseStealObjective = { ent-BaseLivingObjective }
    .desc = { ent-BaseLivingObjective.desc }

ent-BaseSurviveObjective = { ent-BaseObjective }
    .desc = { ent-BaseObjective.desc }

ent-BaseCodeObjective = { ent-BaseObjective }
    .desc = { ent-BaseObjective.desc }

ent-BaseFreeObjective = { ent-BaseObjective }
    .desc = { ent-BaseObjective.desc }

ent-BaseCounterObjective = { ent-BaseObjective }
    .desc = Нас интересует корреспонденция Nanotrasen. Инструмент для вскрытия писем не прилагается.

ent-BaseChangelingObjective = { ent-BaseObjective }
    .desc = { ent-BaseObjective.desc }

ent-ChangelingSurviveObjective = Выживите.
    .desc = Мы должны остаться живыми любой ценой.

ent-ChangelingEscapeIdentityObjective = { ent-BaseObjective }
    .desc = Мы должны скрыться, используя личность этой жертвы с её ID-картой.

ent-ChangelingKillRandomPersonObjective = { ent-BaseObjective }
    .desc = Данная личность наша добыча. Мы должны убедиться, что она останется мертвой на станции.

ent-BaseDragonObjective = { ent-BaseObjective }
    .desc = { ent-BaseObjective.desc }

ent-CarpRiftsObjective = { ent-BaseDragonObjective }
    .desc = { ent-BaseDragonObjective.desc }

ent-DragonSurviveObjective = Выжить
    .desc = Вы должны оставаться в живых, чтобы сохранять контроль.

ent-BaseNinjaObjective = { ent-BaseObjective }
    .desc = { ent-BaseObjective.desc }

ent-DoorjackObjective = { ent-BaseNinjaObjective }
    .desc = { ent-BaseNinjaObjective.desc }

ent-StealResearchObjective = { ent-BaseNinjaObjective }
    .desc = Ваши перчатки могут быть использованы для взлома сервера РНД и кражи его технологий. Если наука буксует, то придётся поработать вам.

ent-SpiderChargeObjective = { ent-BaseNinjaObjective }
    .desc = Эта бомба может быть взорвана в определённом месте. Обратите внимание, что бомба не сработает в другом месте!

ent-NinjaSurviveObjective = Выжить
    .desc = Вы не будете хорошим ниндзя, если умрёте, не так ли?

ent-TerrorObjective = Призвать угрозу
    .desc = Используйте свои перчатки на консоли связи, чтобы навлечь на станцию ещё одну угрозу.

ent-MassArrestObjective = Объявите всех в розыск
    .desc = С помощью перчаток взломайте консоль криминальных записей и объявите всю станцию в розыск!

ent-ParadoxCloneLivingObjective = Улететь на Центком живым и свободным на эвакуационном шаттле.
    .desc = Вернитесь к своей прошлой жизни.

ent-ParadoxCloneKillObjective = Исправить пространственно-временной парадокс.
    .desc = Замените свой оригинал, чтобы исправить парадокс. Помните, ваша миссия — слиться с толпой, не убивайте никого, если это не нужно!

ent-BaseThiefObjective = { ent-BaseObjective }
    .desc = { ent-BaseObjective.desc }

ent-BaseThiefStealObjective = { ent-BaseThiefObjective }
    .desc = { ent-BaseThiefObjective.desc }

ent-BaseThiefStealCollectionObjective = { ent-BaseThiefObjective }
    .desc = { ent-BaseThiefObjective.desc }

ent-BaseThiefStealStructureObjective = { ent-BaseThiefObjective }
    .desc = { ent-BaseThiefObjective.desc }

ent-BaseThiefStealAnimalObjective = { ent-BaseThiefObjective }
    .desc = { ent-BaseThiefObjective.desc }

ent-HeadCloakStealCollectionObjective = { ent-BaseThiefStealCollectionObjective }
    .desc = { ent-BaseThiefStealCollectionObjective.desc }

ent-HeadBedsheetStealCollectionObjective = { ent-BaseThiefStealCollectionObjective }
    .desc = { ent-BaseThiefStealCollectionObjective.desc }

ent-StampStealCollectionObjective = { ent-BaseThiefStealCollectionObjective }
    .desc = { ent-BaseThiefStealCollectionObjective.desc }

ent-DoorRemoteStealCollectionObjective = { ent-BaseThiefStealCollectionObjective }
    .desc = { ent-BaseThiefStealCollectionObjective.desc }

ent-TechnologyDiskStealCollectionObjective = { ent-BaseThiefStealCollectionObjective }
    .desc = { ent-BaseThiefStealCollectionObjective.desc }

ent-MailStealCollectionObjective = { ent-BaseThiefStealCollectionObjective }
    .desc = { ent-BaseThiefStealCollectionObjective.desc }

ent-IDCardsStealCollectionObjective = { ent-BaseThiefStealCollectionObjective }
    .desc = { ent-BaseThiefStealCollectionObjective.desc }

ent-OfficerHandgunsStealCollectionObjective = { ent-BaseThiefStealCollectionObjective }
    .desc = { ent-BaseThiefStealCollectionObjective.desc }

ent-LAMPStealCollectionObjective = { ent-BaseThiefStealCollectionObjective }
    .desc = { ent-BaseThiefStealCollectionObjective.desc }

ent-ClothingEyesHudsStealCollectionObjective = { ent-BaseThiefStealCollectionObjective }
    .desc = { ent-BaseThiefStealCollectionObjective.desc }

ent-ForensicScannerStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-FlippoEngravedLighterStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-ClothingHeadHatWardenStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-WantedListCartridgeStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-ClothingOuterHardsuitVoidParamedStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-MedicalTechFabCircuitboardStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-ClothingHeadsetAltMedicalStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-FireAxeStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-AmePartFlatpackStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-ChiefEngineerToolbeltStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-CargoShuttleCircuitboardStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-BibleStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-ClothingNeckGoldmedalStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-ClothingNeckClownmedalStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-CaptainSwordStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-NuclearBombStealObjective = { ent-BaseThiefStealStructureObjective }
    .desc = { ent-BaseThiefStealStructureObjective.desc }

ent-FaxMachineCaptainStealObjective = { ent-BaseThiefStealStructureObjective }
    .desc = { ent-BaseThiefStealStructureObjective.desc }

ent-ChemDispenserStealObjective = { ent-BaseThiefStealStructureObjective }
    .desc = { ent-BaseThiefStealStructureObjective.desc }

ent-XenoArtifactStealObjective = { ent-BaseThiefStealStructureObjective }
    .desc = { ent-BaseThiefStealStructureObjective.desc }

ent-FreezerHeaterStealObjective = { ent-BaseThiefStealStructureObjective }
    .desc = { ent-BaseThiefStealStructureObjective.desc }

ent-TegStealObjective = { ent-BaseThiefStealStructureObjective }
    .desc = { ent-BaseThiefStealStructureObjective.desc }

ent-BoozeDispenserStealObjective = { ent-BaseThiefStealStructureObjective }
    .desc = { ent-BaseThiefStealStructureObjective.desc }

ent-AltarNanotrasenStealObjective = { ent-BaseThiefStealStructureObjective }
    .desc = { ent-BaseThiefStealStructureObjective.desc }

ent-PlantRDStealObjective = { ent-BaseThiefStealStructureObjective }
    .desc = { ent-BaseThiefStealStructureObjective.desc }

ent-ToiletGoldenStealObjective = { ent-BaseThiefStealStructureObjective }
    .desc = { ent-BaseThiefStealStructureObjective.desc }

ent-IanStealObjective = { ent-BaseThiefStealAnimalObjective }
    .desc = { ent-BaseThiefStealAnimalObjective.desc }

ent-BingusStealObjective = { ent-BaseThiefStealAnimalObjective }
    .desc = { ent-BaseThiefStealAnimalObjective.desc }

ent-McGriffStealObjective = { ent-BaseThiefStealAnimalObjective }
    .desc = { ent-BaseThiefStealAnimalObjective.desc }

ent-WalterStealObjective = { ent-BaseThiefStealAnimalObjective }
    .desc = { ent-BaseThiefStealAnimalObjective.desc }

ent-MortyStealObjective = { ent-BaseThiefStealAnimalObjective }
    .desc = { ent-BaseThiefStealAnimalObjective.desc }

ent-RenaultStealObjective = { ent-BaseThiefStealAnimalObjective }
    .desc = { ent-BaseThiefStealAnimalObjective.desc }

ent-ShivaStealObjective = { ent-BaseThiefStealAnimalObjective }
    .desc = { ent-BaseThiefStealAnimalObjective.desc }

ent-TropicoStealObjective = { ent-BaseThiefStealAnimalObjective }
    .desc = { ent-BaseThiefStealAnimalObjective.desc }

ent-EscapeThiefShuttleObjective = Улететь на Центком живым и свободным на эвакуационном шаттле.
    .desc = Вы же не хотите, чтобы о вашей незаконной деятельности кто-нибудь узнал?

ent-ExpeditionsCircuitboardStealObjective = { ent-BaseThiefStealObjective }
    .desc = { ent-BaseThiefStealObjective.desc }

ent-BaseTraitorObjective = { ent-BaseObjective }
    .desc = { ent-BaseObjective.desc }

ent-BaseTraitorSocialObjective = { ent-BaseTraitorObjective }
    .desc = { ent-BaseTraitorObjective.desc }

ent-BaseTraitorStealObjective = { ent-BaseTraitorObjective }
    .desc = { ent-BaseTraitorObjective.desc }

ent-EscapeShuttleObjective = Улететь на Центком живым и свободным.
    .desc = Один из наших агентов под прикрытием допросит вас по прибытии. Не дайте себя арестовать.

ent-DieObjective = Умереть славной смертью.
    .desc = Умрите.

ent-KillRandomPersonObjective = { ent-BaseTraitorObjective }
    .desc = Сделайте это, как посчитаете нужным. Только убедитесь, что цель не покинет станцию.

ent-KillRandomHeadObjective = { ent-BaseTraitorObjective }
    .desc = Нам нужно, чтобы этот глава исчез, и вы, вероятно, знаете, почему. Убедитесь, что глава не попадёт на Центком даже в мёртвом виде. Удачи, агент.

ent-KillStationAiObjective = {ent-BaseTraitorObjective }
    .desc = Nanotrasen с гордостью заявляет о своей передовой технологии искусственного интеллекта. Напомните им, что это всего лишь игрушка, которую можно сломать.

ent-RandomTraitorAliveObjective = { ent-BaseTraitorSocialObjective }
    .desc = Раскрывать себя или нет — решайте сами. Нам нужно, чтобы он выжил.

ent-RandomTraitorProgressObjective = { ent-BaseTraitorSocialObjective }
    .desc = Раскрывать себя или нет — решайте сами. Нам нужно, чтобы он преуспел.

ent-BaseCMOStealObjective = { ent-BaseTraitorStealObjective }
    .desc = { ent-BaseTraitorStealObjective.desc }

ent-CMOHyposprayStealObjective = { ent-BaseCMOStealObjective }
    .desc = { ent-BaseCMOStealObjective.desc }

ent-CMOCrewMonitorStealObjective = { ent-BaseCMOStealObjective }
    .desc = { ent-BaseCMOStealObjective.desc }

ent-BaseRDStealObjective = { ent-BaseTraitorStealObjective }
    .desc = { ent-BaseTraitorStealObjective.desc }

ent-RDHardsuitStealObjective = { ent-BaseRDStealObjective }
    .desc = { ent-BaseRDStealObjective.desc }

ent-HandTeleporterStealObjective = { ent-BaseRDStealObjective }
    .desc = { ent-BaseRDStealObjective.desc }

ent-EnergyMagnumStealObjective = { ent-BaseTraitorStealObjective }
    .desc = { ent-BaseTraitorStealObjective.desc }

ent-MagbootsStealObjective = { ent-BaseTraitorStealObjective }
    .desc = { ent-BaseTraitorStealObjective.desc }

ent-ClipboardStealObjective = { ent-BaseTraitorStealObjective }
    .desc = { ent-BaseTraitorStealObjective.desc }

ent-KnuckleDustersStealObjective = { ent-BaseTraitorStealObjective }
    .desc = { ent-BaseTraitorStealObjective.desc }

ent-CorgiMeatStealObjective = { ent-BaseTraitorStealObjective }
    .desc = { ent-BaseTraitorStealObjective.desc }

ent-BaseCaptainObjective = { ent-BaseTraitorStealObjective }
    .desc = { ent-BaseTraitorStealObjective.desc }

ent-CaptainIDStealObjective = { ent-BaseCaptainObjective }
    .desc = { ent-BaseCaptainObjective.desc }

ent-CaptainJetpackStealObjective = { ent-BaseCaptainObjective }
    .desc = { ent-BaseCaptainObjective.desc }

ent-CaptainGunStealObjective = { ent-BaseCaptainObjective }
    .desc = { ent-BaseCaptainObjective.desc }

ent-SupercritAnomaliesObjective = { ent-BaseTraitorObjective}
    .desc = Nanotrasen проявляет большой интерес к аномалиям, которые могут иметь потенциально катастрофические последствия. Познакомьте их с огнем, с которым они играют.

ent-HijackTradeStationObjective = Взломайте автоматизированную торговую станцию
    .desc = Вашему аплинку разрешен один маяк взлома. Разместите его на автоматизированной торговой станции и защищайте его, пока он взламывает торговую станцию.

ent-MailFraudObjective = {ent-BaseTraitorObjective}
    .desc = Нас интересует корреспонденция Nanotrasen. Инструмент для вскрытия писем не прилагается.

ent-WizardSurviveObjective = Выжить
    .desc = Федерация Космических Волшебников желает, чтобы вы остались в живых.

ent-WizardDemonstrateObjective = Навести хаос
    .desc = Научите этих станционных олухов никогда больше не проявлять неуважение к волшебнику.

ent-BagelTheaterRoomMarker = Bagel театр интерьер маркер
    .desc = { ent-BaseRoomMarker.desc }

ent-SalvageShuttleMarker = маркер утилизаторский шаттл
    .desc = { ent-FTLPoint.desc }

ent-MaintsRoomMarker = маркер интерьер технические помещения
    .desc = { ent-BaseRoomMarker.desc }

ent-MaintsRoomMarkerClearing = { ent-MaintsRoomMarker }
    .desc = { ent-MaintsRoomMarker.desc }
    .suffix = очистка

ent-VGRoidInteriorRoomMarker = маркер интерьера VGRoid
    .desc = { ent-BaseRoomMarker.desc }

ent-ActionMimeInvisibleWall = Создать невидимую стену
    .desc = Создаёт перед вами невидимую стену, если хватает места.

ent-BaseMindRole = Роль сознания
    .desc = Энтити роли сознания

ent-BaseMindRoleAntag = { ent-BaseMindRole }
    .desc = { ent-BaseMindRole.desc }

ent-MindRoleObserver = Роль наблюдатель
    .desc = { ent-BaseMindRole.desc }

ent-MindRoleGhostRoleNeutral = Роль призрака
    .desc = { ent-BaseMindRole.desc }

ent-MindRoleGhostRoleFamiliar = Роль призрака (Фамильяр)
    .desc = { ent-MindRoleGhostRoleNeutral.desc }

ent-MindRoleGhostRoleFreeAgent = Роль призрака (Свободный агент)
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleGhostRoleFreeAgentHarmless = Роль призрака (Свободный агент)
    .desc = { ent-MindRoleGhostRoleNeutral.desc }

ent-MindRoleGhostRoleSilicon = Роль призрака (Синтетик)
    .desc = { ent-MindRoleGhostRoleNeutral.desc }

ent-MindRoleGhostRoleSiliconAntagonist = Роль призрака (Синтетик антагонист)
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleGhostRoleSoloAntagonist = Роль призрака (Соло-антагонист)
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleGhostRoleTeamAntagonist = Роль призрака (Командный антагонист)
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleGhostRoleTeamAntagonistFlock = Роль призрака (Командный антагонист)
    .desc = { ent-MindRoleGhostRoleTeamAntagonist.desc }

ent-MindRoleJob = Роль работа
    .desc = { ent-BaseMindRole.desc }

ent-MindRoleSiliconBrain = Роль мозг киборга
    .desc = { ent-BaseMindRole.desc }

ent-MindRoleSubvertedSilicon = Роль дефектный синтетик
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleDragon = Роль дракон
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleNinja = Роль космический ниндзя
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleParadoxClone = Роль парадоксальный клон
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleNukeops = Роль ядерный оперативник
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleNukeopsMedic = Роль медик ядерных оперативников
    .desc = { ent-MindRoleNukeops.desc }

ent-MindRoleNukeopsCommander = Роль командир ядерных оперативников
    .desc = { ent-MindRoleNukeops.desc }

ent-MindRoleLoneops = Роль одиночный оперативник
    .desc = { ent-MindRoleNukeops.desc }

ent-MindRoleHeadRevolutionary = Роль глава революции
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleRevolutionary = Роль революционер
    .desc = { ent-MindRoleHeadRevolutionary.desc }

ent-MindRoleSurvivor = Роль выживший
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleThief = Роль вор
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleTraitor = Роль предатель
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleTraitorSleeper = Роль спящий агент
    .desc = { ent-MindRoleTraitor.desc }

ent-MindRoleTraitorReinforcement = Роль подкрепление Синдикат
    .desc = { ent-MindRoleTraitor.desc }

ent-MindRoleWizard = Роль призрак
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleMothershipCore = Роль ядро материнского корабля
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleXenoborg = Роль ксеноборг
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleInitialInfected = Роль нулевой пациент
    .desc = { ent-BaseMindRoleAntag.desc }

ent-MindRoleZombie = Роль зомби
    .desc = { ent-MindRoleGhostRoleTeamAntagonistFlock.desc }

ent-MindRoleChangeling = Роль генокрад
    .desc = { ent-BaseMindRoleAntag.desc }

ent-StorePresetUplink = { "" }
    .desc = { "" }

ent-StorePresetSpellbook = { "" }
    .desc = { "" }

ent-StorePresetChangeling = { "" }
    .desc = { "" }

ent-StorePresetRemoteUplink = { ent-StorePresetUplink }
    .desc = { ent-StorePresetUplink.desc }

ent-BaseXenoArtifactEffect = эффект
    .desc = Неизвестный

ent-BaseOneTimeXenoArtifactEffect = одноразовый эффект
    .desc = Неизвестный

ent-XenoArtifactEffectUniversalIntercom = { ent-BaseOneTimeXenoArtifactEffect }
    .desc = Получает способности устройства дальней связи

ent-XenoArtifactBecomeRandomInstrument = { ent-BaseOneTimeXenoArtifactEffect }
    .desc = Получает способности музыкального инструмента

ent-XenoArtifactStorage = { ent-BaseOneTimeXenoArtifactEffect }
    .desc = Получает способности скрытого хранилища

ent-XenoArtifactPhasing = { ent-BaseOneTimeXenoArtifactEffect }
    .desc = Становится фазированным

ent-XenoArtifactWandering = { ent-BaseOneTimeXenoArtifactEffect }
    .desc = Начинает двигаться рывками

ent-XenoArtifactSolutionStorage = { ent-BaseOneTimeXenoArtifactEffect }
    .desc = Получает способности контейнера для химических веществ

ent-XenoArtifactSpeedUp = { ent-BaseOneTimeXenoArtifactEffect }
    .desc = Повышает скорость движения держателя

ent-XenoArtifactDrill = { ent-BaseOneTimeXenoArtifactEffect }
    .desc = Получает способности бура

ent-XenoArtifactGenerateEnergy = { ent-BaseOneTimeXenoArtifactEffect }
    .desc = Производит электричество

ent-XenoArtifactGun = { ent-BaseOneTimeXenoArtifactEffect }
    .desc = Получает способности огнестрельного оружия

ent-XenoArtifactGhost = { ent-BaseOneTimeXenoArtifactEffect }
    .desc = Становится разумным

ent-XenoArtifactOmnitool = { ent-BaseOneTimeXenoArtifactEffect }
    .desc = Получает способности омнитула

ent-XenoArtifactEffectBadFeeling = { ent-BaseXenoArtifactEffect }
    .desc = Передаёт возвышенное послание

ent-XenoArtifactEffectGoodFeeling = { ent-BaseXenoArtifactEffect }
    .desc = Передаёт возвышенное послание

ent-XenoArtifactEffectJunkSpawn = { ent-BaseXenoArtifactEffect }
    .desc = Создание перерабатываемого хлама

ent-XenoArtifactEffectLightFlicker = { ent-BaseXenoArtifactEffect }
    .desc = Незначительные электромагнитные помехи

ent-XenoArtifactPotassiumWave = { ent-BaseXenoArtifactEffect }
    .desc = Производит калий

ent-XenoArtifactFloraSpawn = { ent-BaseXenoArtifactEffect }
    .desc = Производит флору

ent-XenoArtifactChemicalPuddle = { ent-BaseXenoArtifactEffect }
    .desc = Производит лужи с химическими смесями

ent-XenoArtifactThrowThingsAround = { ent-BaseXenoArtifactEffect }
    .desc = Небольшой импульс

ent-XenoArtifactColdWave = { ent-BaseXenoArtifactEffect }
    .desc = Охлаждает окружающий газ

ent-XenoArtifactHeatWave = { ent-BaseXenoArtifactEffect }
    .desc = Значительно нагревает окружающий газ

ent-XenoArtifactFoamMild = { ent-BaseXenoArtifactEffect }
    .desc = Производит химическую пену

ent-XenoArtifactRandomInstrumentSpawn = { ent-BaseXenoArtifactEffect }
    .desc = Создаёт музыкальный инструмент

ent-XenoArtifactMonkeySpawn = { ent-BaseXenoArtifactEffect }
    .desc = Создаёт примата
