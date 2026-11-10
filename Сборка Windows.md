# Сборка Windows

## Готовый билд вертикального среза

Запуск:

`D:\Build\HamsterAdventures\HamsterAdventures.exe`

Сборка содержит только сцену `Assets/Scenes/Level01_VerticalSlice.unity`, запускается в окне 1600×900 и сразу показывает выбор режима:

- `1` — соло за Хомяка;
- `2` — соло за Хомячиху;
- `3` — кооп.

## Пересобрать

Из Unity: **Tools → Hamster → Build Windows Vertical Slice**.

Или batchmode:

`-executeMethod VerticalSliceBuild.BuildWindows64`

Скрипт сборки: `Assets/Editor/VerticalSliceBuild.cs`.

## Что не входит в билд

- редакторские тестовые сцены;
- сохранения, главное меню и выбор уровней;
- полноценный арт первого уровня;
- звук и музыка.
