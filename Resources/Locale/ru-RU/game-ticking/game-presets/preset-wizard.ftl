## Survivor

roles-antag-survivor-name = Выживший
# It's a Halo reference
roles-antag-survivor-objective = Текущая задача: Выжить
survivor-role-greeting =
    Вы — выживший. Ваша главная задача — остаться в живых и вернуться на Центком.
    Накопите столько огневой мощи, сколько необходимо для гарантии вашего выживания.
    Никому не доверяйте.
survivor-round-end-dead-count =
    { $deadCount ->
        [one] [color=red]{ $deadCount }[/color] выживший умер.
       *[other] [color=red]{ $deadCount }[/color] выживших умерло.
    }
survivor-round-end-alive-count =
    { $aliveCount ->
        [one] [color=yellow]{ $aliveCount }[/color] выживший остался на станции.
       *[other] [color=yellow]{ $aliveCount }[/color] выживших осталось на станции.
    }
survivor-round-end-alive-on-shuttle-count =
    { $aliveCount ->
        [one] [color=green]{ $aliveCount }[/color] выживший выбрался живым.
       *[other] [color=green]{ $aliveCount }[/color] выживших выбралось живыми.
    }

## Wizard

objective-issuer-swf = [color=turquoise]Федерация космических волшебников[/color]
wizard-title = Волшебник
wizard-description = На станции присутствует волшебник! Никогда не знаешь, что они могут натворить.
roles-antag-wizard-name = Волшебник
roles-antag-wizard-objective = Преподайте им урок, который они никогда не забудут.
wizard-role-greeting =
    Время для волшебника, огненный шар!
    Между Федерацией космических волшебников и Nanotrasen возникли противоречия. Вы были выбраны Федерацией Космических Волшебников, чтобы навестить станцию и "напомнить им", почему над заклинателями не издеваются.
    Совершите хаос и разруху! Вам решать, что именно предпринять, но помните, что Космические волшебники желают, чтобы вы вернулись живыми.
wizard-round-end-name = волшебник

## TODO: Wizard Apprentice (Coming sometime post-wizard release)

