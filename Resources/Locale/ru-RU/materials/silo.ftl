ore-silo-ui-title = Хранилище материалов
ore-silo-ui-label-clients = Машины
ore-silo-ui-label-mats = Материалы
ore-silo-ui-itemlist-entry =
    { $linked ->
        [true] { "[Связано] " }
       *[False] { "" }
    } { $name } ({ $beacon }) { $inRange ->
        [true] { "" }
       *[false] (Вне зоны доступа)
    }
