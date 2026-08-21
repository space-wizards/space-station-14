markings-search = Поиск
-markings-selection = { $selectable ->
    [0] Вы больше не можете выбрать черту.
    [one] Вы можете выбрать еще одну черту.
    *[other] Вы можете выбрать ещё { $selectable } черты.
}
markings-limits = { $required ->
    [true] { $count ->
            [-1] Выберите хотя бы одну черту.
            [0] Вы не можете выбрать ещё черту, но как-то, должны? Это баг.
            [one] Выберите одну черту.
            *[other] Выберите хотя бы одну черту и до { $count }. { -markings-selection(selectable: $selectable) }
        }
    *[false] { $count ->
            [-1] Выберите любое количество черт.
            [0] Вы больше не можете выбрать черту.
            [one] Выберите до одной черты.
            *[other] Выберите до { $count } черт. { -markings-selection(selectable: $selectable) }
        }
}
markings-reorder = Выбранные черты

humanoid-marking-modifier-respect-limits = Учитывать ограничения
humanoid-marking-modifier-respect-group-sex = Учитывать ограничение расы и пола
humanoid-marking-modifier-base-layers = Базовый слой
humanoid-marking-modifier-enable = Включить
humanoid-marking-modifier-prototype-id = ID прототипа:

# Categories

markings-organ-Torso = Туловище
markings-organ-Head = Голова
markings-organ-ArmLeft = Левая рука
markings-organ-ArmRight = Правая рука
markings-organ-HandRight = Правая кисть
markings-organ-HandLeft = Левая кисть
markings-organ-LegLeft = Левая нога
markings-organ-LegRight = Правая нога
markings-organ-FootLeft = Левая стопа
markings-organ-FootRight = Правая стопа
markings-organ-Eyes = Глаза

markings-layer-Special = Особое
markings-layer-Tail = Хвост
markings-layer-Tail-Moth = Крылья
markings-layer-Hair = Волосы
markings-layer-FacialHair = Лицевая растительность
markings-layer-UndergarmentTop = Нижняя рубашка
markings-layer-UndergarmentBottom = Трусы
markings-layer-Chest = Туловище
markings-layer-Head = Голова
markings-layer-Snout = Нос
markings-layer-SnoutCover = Нос (Покрытие)
markings-layer-HeadSide = Голова (Бок)
markings-layer-HeadTop = Голова (Верх)
markings-layer-Eyes = Глаза
markings-layer-RArm = Правая рука
markings-layer-LArm = Левая рука
markings-layer-RHand = Правая кисть
markings-layer-LHand = Левая кисть
markings-layer-RLeg = Правая нога
markings-layer-LLeg = Левая нога
markings-layer-RFoot = Правая стопа
markings-layer-LFoot = Левая стопа
markings-layer-Overlay = Наложение
markings-layer-TailOverlay = Наложение

