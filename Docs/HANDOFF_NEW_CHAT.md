# КОНТЕКСТ ДЛЯ НОВОГО ЧАТА (19.11.2026)

## Проект
- Unity 6000.6.5f1, URP 2D, Input System. Проект: `D:\dev\Hamster-Adventures\Hamster Adventures`.
- Основная сцена: `Assets/Scenes/Level01_Full.unity` (18 комнат, единый линейный маршрут).
- **Зона I (комнаты 01–05) принята как эталон** и зафиксирована: `Docs/LEVEL_1_ZONE1_REFERENCE.md`, `LEVEL_1_COLLIDER_BASELINE.txt`, guard-меню.
- **Зона II (06–10) в работе**, комнаты 11–18 не начаты.
- План 5 этапов: `Docs/LEVEL_1_ZONES_II_VI_PLAN.md`.

## Правила работы (важно)
1. После каждой зоны — стоп и приёмка пользователем.
2. **Пользователь правит сцену руками.** Мои «подгонки коллайдеров» запрещены: они заменили его ручные коллайдеры камней на прямоугольники (был конфликт). Не запускать `Zone2FitToVisuals`, `Zone2Sync`, `Zone2Cleanup`, `Zone2RestoreLayout` без прямой просьбы.
3. **Unity должен быть закрыт** для batch-команд. Если открыт — batch молча падает по локам (проверять `Get-CimInstance Win32_Process`).
4. Перед словами «исправил» — проверять визуально/логами из билда. `ScreenCapture` в этом окружении не пишет файл; рабочие способы: редакторный рендер `ZoneShot` (Tools → Хомяк → Зона II: снимки) и физические логи через `-diagstand X` в билде.
5. Изменения коммитить и пушить в `origin/main` (github: AlekseyHorjakov/Hamster-Adventures).

## Сборка
```powershell
$u='C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
& $u -batchmode -nographics -quit -projectPath "D:\dev\Hamster-Adventures\Hamster Adventures" -executeMethod Level01Zone1Build.BuildWindows64 -logFile "C:\Windows\Temp\build.log"
```
Билд: `D:\Build\HamsterAdventures_Level01_Zone1\HamsterAdventures_Level01_Zone1.exe`
(дата `_Data\level0` — реальная дата сборки; exe-стаб не обновляется).

## Полезное
- Меню переименованы в русские: `Tools → Хомяк → ...` (сборка, проверки эталонов, снимки, арт, вода, VFX).
- Арт: `Docs/LEVEL_1_ART_MAP.md`. Водные листы нарезаны: `Assets/Art/Level01/WaterSlices` (139+ слайсов, импорт: Tools → Хомяк → Арт: импортировать водные слайсы).
- Водопад: три анимации каскадов `Z2C_Cascade_1..3` (кадры `Cascade_1..3_01..06`), `Z2_Waterfall` + `Z2W_Splash`, `Z2W_Mist`.
- Озеро: `WaterShore_Edge` (его берег, Ground/7) + `Z2W_LakeBackdrop` (Mid/-19, мягкие края).
- Комната 06: бой `Encounter_Берег` + 3 гиены на берегу (`BeachHyena_*`).

## Известные проблемы
- **Коллайдеры не двигаются вместе с визуалом**, потому что спрайт часто в дочернем `Visual`, а `BoxCollider2D` — на корне. Решение: компонент `PlatformColliderSync` (Assets/Scripts/Level) — повесить на корень, тогда коллайдер сам тянется за спрайтом (в т.ч. при перемещении `Visual`).
- Удалены невидимые опоры: коллайдеры на `Visual`-детях островов и `CrossStart` (x 314–321). Если снова появится «стою на воздухе» — искать сплошной `Collider2D` без видимого спрайта в этой точке.
- Осталось: кромка/декор 06–10 довести, комнаты 11–18, босс 17, финал 18.

## Последние коммиты
- `9e73323` убран невидимый коллайдер CrossStart
- `8e4ec26` убраны коллайдеры на Visual-детях островов
- `75c9904` мосты/доски односторонние
- `c61b7a6` озерный фон, чистка, отладочные прыжки

## Следующие шаги
1. Пользователь сам добавляет окружение в редактор.
2. После его правок: коллайдеры не трогать, только `PlatformColliderSync` по просьбе.
3. Продолжить этап 3: комнаты 11–13 (лес: вход, засада, арена).

## 2026-11-19 — Щука и тонущая секция моста
- `Bridge_Section_02` (x 337.55) получила `SinkingPlatform` — тонет как доски `Bridge_Patch_04`.
- Щука: лист 2172x724 нарезан 6x2 → `Assets/Art/Enemies/Pike/Slices/Pike_Jump_01..12.png` (PPU 100, пивот по низу).
- `PikeLeap` (Assets/Scripts/Combat) стоит в промежутке 343.2–349.7 (x 346.8, низ кадра на линии воды -2.27, scale 0.8, sorting 15). Раз в ~2.8 с выпрыгивает, бьёт на 50% максимального здоровья и отбрасывает; `Health` нет — убить нельзя.
- Проверено PlayMode-тестом `PikeLeapPlayModeTests`: здоровье 100 → 50. Запуск: `-runTests -testPlatform playmode -testFilter PikeLeapPlayModeTests` (без `-quit`).
- Инструменты: `Tools → Хомяк → Зона II: щука и тонущая секция` (идемпотентный setup), `... проверить посадку щуки` (PikeCheck).
