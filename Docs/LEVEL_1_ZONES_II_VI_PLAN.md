# ПЛАН: СЛЕДУЮЩИЕ 5 ЭТАПОВ (КОМНАТЫ 06-18) - РЕД. 2

Сверено с задачей, `LEVEL_1_MAP.md`, `LEVEL_1_GEOMETRY.md`, GitHub (`origin/main` = cceeaa5)
и **локальной папкой спрайтов**. Вывод: арт для комнат 06-18 уже есть, новых ассетов не ждём,
плейсхолдеры не нужны.

## 0. Инвентаризация арта (локально)

- `Assets/Art/Level01/ModularSlices` - 159 готовых слайсов.
- `Assets/Art/Level01/ModularSources` - 43 исходника: Water (12), Interactables (6), Props (7),
  VFX (6), Ground (4), Platforms (3), UI (3), Bridge (1), Structures (1).
- `Assets/Resources/Level01` - фоны и крупные листы: Lake, Forest, Sky, Mountains, Ground,
  DirtFill, Platform, ArenaFence, FinishGate, Boss, CampWorld, Foreground, Props.
- Вода: `WaterSurface_Tile`, `WaterShore_Edge`, `Waterfall_Column`, `SteppingStone_01-08`,
  `LilyPad`, `Reeds_Cattails`, `Reeds_Rocks`, `RiverRocks`, `Seabed`, `UnderwaterFloor`,
  `ShoreTiles`, `StonePaths_Water`, `WaterRocksPlants`, `WaterSplash_Set_01/02`, `WaterSplash_Hit`.
- Рельеф: `GroundTile_Long`, `GroundFill_Tile`, `GroundEdge_SlopeA/B`, `GroundEdge_CliffA-E`,
  `Slope_Up/Down`, `MossyCliffs_Set`, `Cliff_Chunk_A/B`, `PitFloor`.
- Мосты/платформы: `Bridge_Section_01-03`, `Bridge_Patch_01`, `Platform_End/Small/Wide`,
  `FloatingPlatform_Set`, `WoodDock_Set_01/02`, `BrokenBridge_Set`.
- Интерактив: `Checkpoint_Campfire_Lit/Unlit`, `Reward_Acorn_01-06`, `Reward_Coin`,
  `Barrel/Crate_*` (целые/треснувшие/обломки), `BreakableBarrel/Crate`, `Campfire_Set`.
- Финал: `Boss` (HyenaBoss 30 кадров), `BossHealth_Frame`, `BossSlam_Telegraph`,
  `ArenaFence`, `FinishGate`.

Итого: всё, что нужно зонам II-IV, лежит в проекте. Осталось собрать сцены и системы.

## 1. Этапы

| # | Этап | Комнаты | X | Поверхность | Арт (уже есть) | Работа | Объём |
|---:|---|---|---|---|---|---|---|
| 1 | Берег и мелководье | 06-07 | 202-274 | 2 → 0.1 → 1.0 | `GroundEdge_SlopeA/B`, `ShoreTiles`, `WaterSurface_Tile`, `WaterShore_Edge`, `SteppingStone_01-08`, `Reeds_*`, `Seabed`, `UnderwaterFloor`, `WaterSplash_Set_01` | спуск-склон 06, кромка воды, неглубокое мелководье 07, камни-степпинг, чекпоинт 06, RoomDefinition 06-07 | S |
| 2 | Острова, переправа, водопад | 08-10 | 274-390 | 2-4.5 → 5 → 8 | `Platform`/`FloatingPlatform_Set`, `StonePaths_Water`, `RiverRocks`, `Waterfall_Column`, `WaterRocksPlants`, `MossyCliffs_Set`, `WoodDock_Set_01` | острова 08, длинные прыжки 09 на доках, ярусный подъём водопада 10, гиены 08/09/10, гейт 08→09, чекпоинт 11 | M |
| 3 | Лес: вход, засада, арена | 11-13 | 390-506 | 8 → 6 | `Forest`, `ForestTree_Large/Mossy`, `ForestBush`, `ArenaFence`, `Checkpoint_Campfire` | смена фона, вход 11, засада 12 (3 гиены), арена 13 (4 гиены + волна), закрытие прохода, чекпоинт 11 | M |
| 4 | Стоянка, тропа, преддверие | 14-16 | 506-610 | 6 → 0 | `CampWorld`, `CampProps_Set_02`, `Camp*` слайсы, `Campfire_Set`, `WoodStructures_Set`, `TrapDebris`, `FloatingPlatform_Set`, `ArenaFence` | стоянка 14 с наградой, тропа 15: обрывы + обрушающиеся платформы (без шипов - арт шипов отсутствует), 2 усиленные гиены, ворота 16, чекпоинт 16 | S/M |
| 5 | Арена босса и выход | 17-18 | 610-696 | 0 | `Boss` (HyenaBoss 30 кадров), `BossHealth_Frame`, `BossSlam_Telegraph`, `ArenaFence`, `FinishGate`, `LandingDust` | арена 17, ворота, бой 140 HP, чекпоинт после босса, финиш 18, экран итогов | M |

## 2. Системы (код)

- `RoomDefinition` 06-18 по мастер-сетке (X, surfaceY, камера).
- `Level01ArtLibrary`: вынести помощники Зоны I (ground runs, ямы, рейкаст декора, платформы).
- `ZoneNArtRevamp`: идемпотентная сборка каждой зоны, как в Зоне I.
- Вода: `ShallowWaterZone` (визуал + брызги `WaterSplash_Hit`, без урона), вода без коллайдеров.
- Водопад: `WaterfallColumn` декор + `FloatingPlatform`/`SteppingStone` как опоры подъёма.
- Ловушки: `CrumblingPlatform` (обрушение через 1.5 с, респавн через 3 с, VFX `TrapDebris`).
- Энкаунтеры: волны в `EncounterController` (13), гейты 08→09, 12→13, 13→14, 17.
- `GameProgress` → чекпоинты по `roomId`, миграция v1/v2.
- `ZoneFadeLayer` по зонам, `BossHealth_Frame` для босса, экран итогов для 18.

## 3. Definition of Done этапа

1. Маршрут проходим соло и в кооперативе, комнаты соединены без разрывов.
2. Коллайдеры совпадают с `LEVEL_1_GEOMETRY.md`, декор не висит в воздухе, вода не мешает.
3. `ZoneNReferenceGuard` = PASS, PlayMode-тесты этапа зелёные.
4. Билд пересобран, кадры из запущенного exe, короткий отчёт, стоп до согласования.

## 4. Риски (арт исключён)

- Вода: визуал легко расходится с коллайдерами - коллайдеры только земля/камни/доки.
- Водопад: вертикальная камера; сначала подъём, потом декор.
- Кооп: гейты открываются только когда мертвы все гиены и оба игрока в строю.
- Сейвы: миграция чекпоинтов на roomId.
- Босс: баланс 140 HP проверить после сборки арены.

## 5. Нужно подтвердить

1. Разбиение на 5 этапов (официально зон осталось 3: II-IV).
2. Старт этапа 1 (комнаты 06-07) сразу, арт уже на месте.
