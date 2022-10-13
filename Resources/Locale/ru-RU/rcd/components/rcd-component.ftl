### UI

# Shown when an RCD is examined in details range
rcd-component-examine-detail-count =
    Находится в режиме { $mode }, и { $ammoCount ->
       *[zero] не содержит зарядов.
        [one] содержит 1 заряд.
        [few] содержит { $ammoCount } заряда.
        [other] содержит { $ammoCount } зарядов.
    }

### Interaction Messages

# Shown when changing RCD Mode
rcd-component-change-mode = РСУ переключён в режим { $mode }.
rcd-component-no-ammo-message = В РСУ закончились заряды!
rcd-component-tile-obstructed-message = Этот тайл заблокирован!
rcd-component-deconstruct-target-not-on-whitelist-message = Вы не можете это деконструировать!
rcd-component-cannot-build-floor-tile-not-empty-message = Пол можно построить только в космосе!
rcd-component-cannot-build-wall-tile-not-empty-message = Вы не можете построить стену в космосе!
rcd-component-cannot-build-airlock-tile-not-empty-message = Вы не можете построить шлюз в космосе!
