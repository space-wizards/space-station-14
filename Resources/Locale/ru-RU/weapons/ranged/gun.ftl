gun-selected-mode-examine = Выбран режим огня [color={ $color }]{ $mode }[/color].
gun-fire-rate-examine = Скорострельность [color={ $color }]{ $fireRate }[/color] в секунду.
gun-selector-verb = Изменить на { $mode }
gun-selected-mode = Выбран { $mode }
gun-disabled = Вы не можете использовать оружие!
gun-clumsy = Оружие взрывается вам в лицо!
gun-set-fire-mode = Выбран режим { $mode }
gun-magazine-whitelist-fail = Это не помещается в оружие!
gun-magazine-fired-empty = Нет патронов!
# SelectiveFire
gun-SemiAuto = полуавто
gun-Burst = очередь
gun-FullAuto = авто
# BallisticAmmoProvider
gun-ballistic-cycle = Перезарядка
gun-ballistic-cycled = Перезаряжено
gun-ballistic-cycled-empty = Разряжено
gun-ballistic-transfer-invalid = { CAPITALIZE($ammoEntity) } нельзя поместить в { $targetEntity }!
gun-ballistic-transfer-empty = В { CAPITALIZE($entity) } пусто.
gun-ballistic-transfer-target-full = { CAPITALIZE($entity) } уже полностью заряжен.
# CartridgeAmmo
gun-cartridge-spent = Он [color=red]израсходован[/color].
gun-cartridge-unspent = Он [color=lime]не израсходован[/color].
# BatteryAmmoProvider
gun-battery-examine =
    Заряда хватит на [color={ $color }]{ $count }[/color] { $count ->
        [one] выстрел
        [few] выстрела
       *[other] выстрелов
    }.
# CartridgeAmmoProvider
gun-chamber-bolt-ammo = Затвор не закрыт
gun-chamber-bolt = Затвор [color={ $color }]{ $bolt }[/color].
gun-chamber-bolt-closed = Затвор закрыт
gun-chamber-bolt-opened = Затвор открыт
gun-chamber-bolt-close = Закрыть затвор
gun-chamber-bolt-open = Открыть затвор
gun-chamber-bolt-closed-state = открыт
gun-chamber-bolt-open-state = закрыт
gun-chamber-rack = Передёрнуть затвор
# MagazineAmmoProvider
gun-magazine-examine =
    Тут [color={ $color }]{ $count }[/color] { $count ->
        [one] штука
        [few] штуки
       *[other] штук
    }.
# RevolverAmmoProvider
gun-revolver-empty = Разрядить револьвер
gun-revolver-full = Револьвер полностью заряжен
gun-revolver-insert = Заряжен
gun-revolver-spin = Вращать барабан
gun-revolver-spun = Барабан вращается
gun-speedloader-empty = Спидлоадер пуст
# GunSpreadModifier
examine-gun-spread-modifier-reduction = The spread has been reduced by [color=yellow]{ $percentage }%[/color].
examine-gun-spread-modifier-increase = The spread has been increased by [color=yellow]{ $percentage }%[/color].
