ore-silo-ui-title = Material Silo
ore-silo-ui-label-clients = Machines
ore-silo-ui-label-mats = Materials
ore-silo-ui-itemlist-entry =
    { $linked ->
        [true] { "[Linked] " }
       *[False] { "" }
    } { $name } ({ $beacon }) { $inRange ->
        [true] { "" }
       *[false] (Out of Range)
    }
