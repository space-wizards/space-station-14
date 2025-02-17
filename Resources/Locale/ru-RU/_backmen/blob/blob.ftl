# Popups
blob-target-normal-blob-invalid = Неподходящий тип блоба, необходимо выбрать нормального блоба.
blob-target-factory-blob-invalid = Неподходящий тип блоба, необходимо выбрать фабрику.
blob-target-node-blob-invalid = Неподходящий тип блоба, необходимо выбрать узел.
blob-target-close-to-resource = Слишком близко к другому ресурсному тайлу.
blob-target-nearby-not-node = Рядом нету узла или ядра.
blob-target-close-to-node = Слишком близко к другому узлу.
blob-target-already-produce-blobbernaut = Данная фабрика уже произвела блоббернаута.
blob-cant-split = Вы не можете разделить ядро.
blob-not-have-nodes = У вас нету узлов.
blob-not-enough-resources = Не хватает ресурсов для действия.
blob-swap-chem = В разработке.
blob-mob-attack-blob = Вы не можете атаковать блоба.
blob-get-resource = +{ $point }
blob-spent-resource = -{ $point }
blobberaut-not-on-blob-tile = Вы умираете без тайлов блоба под ногами.
blobberaut-factory-destroy = Ваша фабрика была разрушена, вы умираете.
carrier-blob-alert = У вас осталось { $second } секунд до превращения.
blob-mob-zombify-second-start = { $pod } начинает превращать вас в зомби
blob-mob-zombify-third-start = { $pod } начинает превращать { $target } в зомби
blob-mob-zombify-second-end = { $pod } превращает вас в зомби
blob-mob-zombify-third-end = { $pod } превращает { $target } в зомби
# UI
blob-chem-swap-ui-window-name = Смена химиката
blob-chem-reactivespines-info =
    Реактивные шипы
    Наносит 25 единиц брут урона.
blob-chem-blazingoil-info =
    Пылающее масло
    Наносит 15 урона ожогами и поджигает цели.
    Делает вас уязвимым к воде.
blob-chem-regenerativemateria-info =
    Регенеративная Материя
    Наносит 15 единиц урона ядами.
    Ядро востанавливает здоровье в 10 раз быстрее и дает на 1 очко больше.
blob-chem-explosivelattice-info =
    Взрывная решетка
    Наносит 5 единиц урона ожогами и взрывает цель, нанося 10 брут урона.
    Споры при смерти взрываются.
    Вы получаете имунитет к взрывам.
    Вы получаете на 50% больше урона ожогами и электричеством.
blob-chem-electromagneticweb-info =
    Электромагнитная паутина
    Наносит 20 урона ожогами, 20% шанс вызывать ЭМИ разряд при атаке.
    Любая уничтоженая плитка гарантировано вызовет ЭМИ.
    Вы получаете на 25% больше урона теплом и брутом.
blob-alert-out-off-station = Блоб был удален т.к. был обнаружен вне станции!

# game-rule

blob-title = Блоб
blob-description = Показатели биологической безопасности станции показывают наличие на станции некого паразита...
# Announcment
blob-alert-recall-shuttle = Эвакуационный шатл не может быть отправлен на станцию пока существует биологическая угроза 5 уровня.
blob-alert-detect = На станции была обнаружена биологическая угроза 5 уровня, обьявлена изоляция станции.
blob-alert-critical = Биологическая угроза достигла критической массы, вам отправлены коды от ядерной боеголовки, вы должны немедленно взорвать станцию.
# Ghost role
blob-carrier-role-name = Носитель блоба
blob-carrier-role-desc = Сущность зараженная "блобом".
blob-carrier-role-rules =
    Вы антагонист. У вас есть 4 минуты перед тем как вы превратитесь в блоба.
    Найдите за это время укромное место для стартовой точки заражения станции, ведь вы очень слабы в первые минуты после создания ядра.
blob-carrier-role-greeting = Вы носитель Блоба. Найдите укромное место на станции и превратитесь в Блоба. Превратите станцию в массу, а ее обитателей в ваших слуг. Все мы Блоб.
# Verbs
blob-pod-verb-zombify = Зомбировать
blob-verb-upgrade-to-strong = Улучшить до сильного блоба
blob-verb-upgrade-to-reflective = Улучшить до отражающего блоба
blob-verb-remove-blob-tile = Убрать блоба
# Alerts
blob-resource-alert-name = Ресурсы ядра
blob-resource-alert-desc = Ваши ресурсы которые производят ресурсные блобы и само ядро, требуются для разрастанция и особых блобов.
blob-health-alert-name = Здоровье ядра
blob-health-alert-desc = Здоровье вашего ядра. Если оно опустится до 0 вы умрёте.
# Greeting
blob-role-greeting =
    Вы блоб - космический паразит который захватывает станции.
    Ваша цель - стать как можно больше не дав себя уничтожить.
    Используйте горячие клавиши Alt+LMB чтобы улучшать обычные плитки до сильных а сильные до отражающих.
    Позаботьтесь о получении ресурсов с блобов ресурсов.
    Вы практически неуязвимы к физическим повреждениям, но опасайтесь теплового урона.
    Учтите что особые клетки блоба работают только возле узлов или ядра.
blob-zombie-greeting = Вы были заражены спорой блоба которая вас воскресила, теперь вы действуете в интересах блоба.
# End round
blob-round-end-result =
    { $blobCount ->
        [one] Был один блоб.
       *[other] Было { $blobCount } блобов.
    }
blob-user-was-a-blob = [color=gray]{ $user }[/color] был блобом.
blob-user-was-a-blob-named = [color=White]{ $name }[/color] ([color=gray]{ $user }[/color]) был блобом.
blob-was-a-blob-named = [color=White]{ $name }[/color] был блобом.
objective-issuer-blob = [color=#33cc00]Блоб[/color]
blob-user-was-a-blob-with-objectives = [color=gray]{ $user }[/color] был блобом и имел следующие цели:
blob-user-was-a-blob-with-objectives-named = [color=White]{ $name }[/color] ([color=gray]{ $user }[/color]) был блобом и имел следующие цели:
blob-was-a-blob-with-objectives-named = [color=White]{ $name }[/color] был блобом и имел следующие цели:
# Objectivies
objective-condition-blob-capture-title = Захватить станцию
objective-condition-blob-capture-description = Ваша единственная цель - полное и безоговорочное поглощение станции. Вам необходимо владеть как минимум { $count } тайлами блоба.
roles-antag-blob-name = Блоб
objective-condition-success = { $condition } | [color={ $markupColor }]Успех![/color]
objective-condition-fail = { $condition } | [color={ $markupColor }]Провал![/color] ({ $progress }%)
