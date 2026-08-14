# SpaceArena

Space Station 14 — ремейк SS13, работающий на движке [RobustToolbox](https://github.com/space-wizards/RobustToolbox), написанном на C#.

SpaceArena — международный PVP билд.

## Ссылки

[Репозиторий SpaceArena](https://github.com/Weh-Group/SpaceArena) | [Документация SS14](https://docs.spacestation14.com/) | [RobustToolbox](https://github.com/space-wizards/RobustToolbox) | [Issues](https://github.com/Weh-Group/SpaceArena/issues)

## Лицензия

Код репозитория распространяется на условиях лицензии, указанной в [LICENSE.TXT](LICENSE.TXT). Код и компоненты, унаследованные от Space Wizards Federation, Space Station 14 и RobustToolbox, сохраняют оригинальные лицензии и авторские права.

Большинство ассетов лицензировано по CC-BY-SA 3.0, если в метаданных не указано иное. Некоторые ассеты могут распространяться по некоммерческим лицензиям CC-BY-NC-SA 3.0 или аналогичным. Для коммерческого использования необходимо проверить и удалить несовместимые ассеты.

Подробнее:

- [Robust Generic Attribution](https://docs.spacestation14.com/en/specifications/robust-generic-attribution.html)
- [Robust Station Image](https://docs.spacestation14.com/en/specifications/robust-station-image.html)

## Документация

Основная документация по разработке и запуску находится в [документации SS14](https://docs.spacestation14.com/). Инструкции проекта описаны в [CONTRIBUTING.md](CONTRIBUTING.md).

## Контрибьют

Перед крупными изменениями рекомендуется создать Issue или обсудить их с командой SpaceArena. Pull request должен содержать описание изменений и инструкции по проверке.

## Сборка

1. Склонируйте репозиторий:

```shell
git clone https://github.com/Weh-Group/SpaceArena.git
cd SpaceArena
```

2. Инициализируйте подмодули и зависимости:

```shell
python RUN_THIS.py
```

3. Соберите проект:

```shell
dotnet build SpaceStation14.slnx
```

Для сборки серверного архива:

```shell
dotnet build Content.Packaging --configuration Release
dotnet run --project Content.Packaging server --hybrid-acz --platform linux-x64
```

Готовый архив появится в каталоге `release/`.
