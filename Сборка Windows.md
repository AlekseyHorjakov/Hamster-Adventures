# Сборка Windows

## Готовый билд вертикального среза

Запуск:

`D:\Build\HamsterAdventures_SliceLatest\HamsterAdventures_SliceLatest.exe`

Сборка запускается в окне 1600×900 со стартового экрана `Assets/Scenes/MainMenu.unity`, затем загружает `Assets/Scenes/Level01_VerticalSlice.unity`. В правом нижнем углу отображается номер сборки — по нему можно убедиться, что запущена свежая версия:

- «Новая игра» → «Один игрок» или «Два игрока»;
- в одиночном режиме герой выбирается стрелками или кликом по персонажу;
- «Продолжить» загружает последнюю контрольную точку.

## Стартовая сцена

Сцена меню собирается через **Tools → Hamster → Build Main Menu** или batchmode `-executeMethod MainMenuBuilder.BuildMainMenu`.

## Пересобрать

Из Unity: **Tools → Hamster → Build Windows Vertical Slice**.

Или batchmode:

`-executeMethod VerticalSliceBuild.BuildWindows64`

Скрипт сборки: `Assets/Editor/VerticalSliceBuild.cs`.

## Что не входит в билд

- редакторские тестовые сцены;
- сохранения, выбор уровней, лагерь и полноценные настройки меню;
- полноценный арт первого уровня;
- звук и музыка.
