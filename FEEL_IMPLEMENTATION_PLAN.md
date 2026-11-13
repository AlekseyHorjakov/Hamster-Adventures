# FEEL IMPLEMENTATION PLAN — Character Feel и Combat Feel

Дата: 2026-11-13  
Основа: `SILKSONG_FEEL_RESEARCH.md`, `HAMSTER_FEEL_AUDIT.md`  
Статус: план реализации; после него начинается изменение существующих систем.

## 1. Границы изменений

Сохраняем:

- строгую 2D-физику и `Rigidbody2D`;
- текущую архитектуру `HamsterMovement` → `AttackController` → `Hitbox` → `Health` → `HitReaction`;
- `HitData` как ScriptableObject параметров удара;
- Input Actions и текущую раскладку;
- кооператив, камеру, HUD, паузу и сохранения;
- существующие спрайты и анимационные клипы.

Не создаём:

- второй `HitStop`;
- второй `CameraShake`;
- второй `Health`;
- второй `AttackController`;
- альтернативную систему движения;
- новые Input Actions без необходимости.

## 2. Character Feel — изменения

### 2.1. `HamsterMovement.cs`

Добавить настраиваемые поля:

- `groundAcceleration` — start value `75`;
- `groundDeceleration` — start value `100`;
- `turnAcceleration` — start value `130`;
- `coyoteTime` — start value `0.10`;
- `jumpBufferTime` — start value `0.12`;
- `jumpCutMultiplier` — start value `0.52`;
- `riseGravityMultiplier` — start value `1.0`;
- `fallGravityMultiplier` — start value `1.55`;
- `maxFallSpeed` — start value `26`;
- `landingFeedbackThreshold` — start value `10`;
- `airStopMultiplier` — start value `0.65`.

Добавить события:

- `Jumped`;
- `Landed(float impactSpeed)`;
- `StartedMoving`;
- `StoppedMoving`;
- `DirectionChanged(int facing)`.

Логика:

- `Update`:
  - читать ввод;
  - обновлять jump buffer;
  - обрабатывать coyote time;
  - выполнять прыжок с учётом буфера;
  - при отпускании кнопки в early-jump окне применить jump cut;
  - обновлять визуальное состояние.
- `FixedUpdate`:
  - на земле: `MoveTowards` к `targetSpeed` с `groundAcceleration`, `groundDeceleration` или `turnAcceleration`;
  - в воздухе: `MoveTowards` с air-параметрами;
  - менять `Rigidbody2D.gravityScale` на `baseGravityScale * rise/fall multiplier`;
  - ограничивать `linearVelocity.y` значением `-maxFallSpeed`;
  - сохранять partner-ограничения.
- `OnCollisionEnter/Stay`:
  - обновлять `isGrounded`, `coyoteUntil`, сбрасывать прыжки;
  - при переходе из воздуха в землю вызывать `Landed` с `Mathf.Abs(previousVelocityY)`.
- `ConsumeJumpPress` не нужен: jump обрабатывается внутри `HamsterMovement`.

Проверить, что `PlayerCombatState.SetControlLocked(true)` и `AttackController` не отключают coyote/buffer непреднамеренно.

### 2.2. `CharacterMotionVisual.cs` — новый компонент

Добавить на `Hamster.prefab` и `HamsterGirl.prefab`.

Параметры:

- `maxLeanDegrees = 8`;
- `leanSmooth = 12`;
- `landingSquashY = 0.86`;
- `landingStretchX = 1.08`;
- `landingSquashDuration = 0.10`;
- `minLandingImpact = 6`;
- `dustPrefab` — опционально `HitSpark`, если отдельный dust не создан;
- `dustTint = (0.65, 0.55, 0.42, 0.55)`;
- `dustScale = 0.55`;
- `runBobAmplitude = 0.015`;
- `landingCameraShakeThreshold = 16`;
- `landingCameraShakeStrength = 0.08`.

Логика:

- в `LateUpdate` читать `HamsterMovement` и `Rigidbody2D`;
- наклонять `Visual` по скорости/направлению ввода;
- при `Landed`:
  - запускать короткий squash/stretch;
  - спавнить пыль;
  - при сильном приземлении делать слабую тряску камеры;
- не изменять позицию/rotation `Visual`, пока активен `HitReaction`;
- не блокировать управление;
- работать независимо у каждого игрока.

Почему отдельный компонент: `HamsterMovement` отвечает за физику, `CharacterMotionVisual` — за презентацию; это не дублирует архитектуру движения.

### 2.3. Анимационные ресурсы

Новые клипы не обязательны на первом проходе:

- `Idle`, `HGo`, `HJump`, `HFall`, `Hurt`, удары уже есть;
- `Land` и `Turn` можно заменить процедурным squash/lean без новых спрайтов.

Если после Play Mode потребуется отдельный `Land`, подготовить:

- `Hamster_Land_Sheet.png`;
- `HamsterGirl_Land_Sheet.png`;
- 4–6 кадров, прозрачный фон, PPU 100.

### 2.4. Проверка Character Feel

- старт движения: виден разгон, нет ощущения льда;
- остановка: короткое торможение, нет проскальзывания на пол-экрана;
- разворот: персонаж быстро меняет направление, есть наклон, нет блока управления;
- coyote: прыжок сразу после схода с платформы;
- jump buffer: нажатие до приземления срабатывает;
- variable jump: короткое нажатие ниже полного;
- падение: ускоренное, но не «каменное»;
- приземление: видимая squash-реакция и пыль;
- оба героя ведут себя одинаково;
- кооп: партнёрские ограничения не ломают прыжок и разворот.

## 3. Combat Feel — изменения

### 3.1. `HitData.cs`

Добавить:

- `HitImpact.impactWeight` — `Light`, `Medium`, `Heavy`, `Launch`;
- `hitFlashColor`;
- `hitFlashDuration`;
- `sparkScale`;
- `sparkTint`;
- `sparkCount`;
- `cameraShakeStrength`;
- `cameraShakeDuration`;
- `hitReactionTilt`;
- `hitReactionRecovery`.

Значения по умолчанию должны быть безопасными для старых ассетов. Ударные ассеты настроить отдельно:

| Asset | Weight | HitStop | Camera | Spark |
|---|---|---:|---|---|
| `HitData_Light1` | Light | 0.05 | 0.08 / 0.08 | 0.9 |
| `HitData_Light2` | Medium | 0.06 | 0.11 / 0.10 | 1.0 |
| `HitData_Strong` | Launch | 0.14 | 0.28 / 0.18 | 1.35 |
| `HitData_Air` | Medium | 0.08 | 0.12 / 0.10 | 1.05 |
| `HitData_Charged` | Launch | 0.16 | 0.38 / 0.24 | 1.55 |
| `HitData_HyenaBite` | Medium | 0.06 | 0.12 / 0.10 | 1.0 |
| `HitData_ThrownPartner` | Heavy | 0.10 | 0.30 / 0.20 | 1.35 |

### 3.2. `DamageInfo.cs`

Добавить:

- `impactWeight`;
- `hitFlashColor`;
- `hitFlashDuration`;
- `hitReactionTilt`;
- `hitReactionRecovery`;
- `sparkCount`.

### 3.3. `Hitbox.cs`

Изменить только конвейер попадания:

- вычислять точку контакта через `collider.ClosestPoint(center)`;
- передавать её в `DamageInfo.hitPoint`;
- передавать impact-параметры;
- спавнить искру в точке контакта, а не в центре хитбокса;
- варьировать цвет/масштаб/количество искр;
- вызывать `HitStop.Play(data.hitStop)` и `CameraFollow.Shake(data.cameraShakeStrength, data.cameraShakeDuration)`;
- не запускать эффект по таймеру вне реального попадания.

### 3.4. `HitReaction.cs`

Переписать `ShortKnockback` в единый контролируемый state machine:

- `Light` — короткий flinch, гашение скорости, маленькая вспышка, без полёта;
- `Medium` — отшатывание с небольшим подскоком, средняя вспышка;
- `Heavy` — сильное отбрасывание, наклон, сильная вспышка, recovery;
- `Launch` — полёт по `launchVelocity`, контролируемый визуальный наклон, приземление, короткая стабилизация;
- не использовать случайное вращение;
- не застревать в геометрии: использовать физику `Rigidbody2D`, не телепортировать тело;
- при повторном попадании не перезапускать полёт, если цель уже летит;
- отменять корутину при смерти;
- восстанавливать `visual.localRotation` и scale после реакции;
- `IsStunned` и `IsLaunched` остаются публичными.

Взаимодействие с AI:

- `HyenaAI` уже останавливает горизонтальное движение при `IsStunned/IsLaunched`;
- `HurtRoutine` не должен возвращать Idle, пока `HitReaction.IsLaunched` истинно;
- при `Heavy/Launch` AI не должен продолжать атаку.

### 3.5. Игроки и i-frames

- `Health.invulnerabilityAfterHit` для героев — `0.55`;
- для гиены — `0.08`;
- `Health` получает `InvulnerableRemaining` и `IsInvulnerable`;
- добавить `HitReaction` на `Hamster.prefab` и `HamsterGirl.prefab`;
- `PlayerCombatState` синхронизирует Hurt-анимацию с `hitStun`/i-frames;
- при повторном попадании в i-frames Hurt не перезапускается;
- добавить визуальное мерцание на время i-frames через `HitReaction` или общий визуальный контроллер.

### 3.6. Атака и шаг

- `StartAttack` не должен позволять `HamsterMovement.FixedUpdate` обнулять шаг атаки;
- на время `movementLock` включить внешний горизонтальный контроль или отдельный attack-momentum API;
- после завершения — вернуть управление;
- `attackDurations` привести к фактическим длинам клипов:
  - `FastPunch ≈ 0.264`;
  - `Hook ≈ 0.225`;
  - `Spin ≈ 0.600`;
  - `JumpKick ≈ 0.225`;
- оставить буферизацию ввода;
- сохранить возможность запуска следующего удара после active/recovery;
- хитбокс включается только Animation Event, кроме заряженного рывка, где он контролируется кодом.

### 3.7. Заряженная атака

- сохранить `minChargeTime = 3`;
- при `Burst` усилить контраст вспышки и impact;
- `HitData_Charged` настроить как `Launch`;
- на попадании — сильная, но не бесконечная тряска, увеличенный spark, длинный hit stop;
- не увеличивать одновременно все эффекты; главный акцент — вспышка + отбрасывание + короткий hit stop.

### 3.8. VFX

- `HitSpark` получает метод настройки цвета, размера, направления;
- искра спавнится в точке контакта;
- добавить пыль приземления через `CharacterMotionVisual`;
- не заменять существующие анимации и не плодить десятки новых частиц.

### 3.9. Звук

Аудиоресурсы отсутствуют. В рамках этой реализации:

- не добавлять случайные звуки;
- подготовить точку вызова звука в `Hitbox`/`HitReaction` только если появится `AudioSource`/клип;
- зафиксировать отсутствие аудио как отдельное ограничение в итоговом отчёте.

## 4. Файлы, которые будут изменены

- `Assets/Scripts/HamsterMovement.cs`
- `Assets/Scripts/Combat/HitData.cs`
- `Assets/Scripts/Combat/DamageInfo.cs`
- `Assets/Scripts/Combat/Hitbox.cs`
- `Assets/Scripts/Combat/HitReaction.cs`
- `Assets/Scripts/Combat/Health.cs`
- `Assets/Scripts/Combat/PlayerCombatState.cs`
- `Assets/Scripts/Combat/HyenaAI.cs`
- `Assets/Scripts/Combat/HitSpark.cs`
- `Assets/Scripts/Combat/AttackController.cs`
- `Assets/Scripts/CharacterMotionVisual.cs` — новый
- `Assets/Editor/FeelSystemsSetup.cs` — новый инструмент настройки префабов/ассетов
- `Assets/Prefabs/Hamster.prefab`
- `Assets/Prefabs/HamsterGirl.prefab`
- `Assets/Prefabs/Hyena.prefab`
- `Assets/Combat/HitData_*.asset`

Сцены не пересобираются; изменения префабов подхватываются существующими сценами.

## 5. Тесты

### Character Feel

- W/A/D/S и альтернативные клавиши обоих героев;
- короткое и длинное нажатие прыжка;
- сход с платформы и прыжок в coyote-окне;
- нажатие прыжка до приземления;
- разворот на полной скорости;
- приземление с разной высоты;
- одновременный прыжок двух игроков;
- расхождение игроков по X/Y и камера.

### Combat Feel

- все три удара комбо;
- попадание в манекен и гиену;
- третий удар с launch;
- заряженный удар по одной и нескольким целям;
- удар в воздухе;
- получение урона игроком;
- повторное попадание во время i-frames;
- смерть гиены в полёте;
- бросок напарника;
- hit stop, spark, camera shake и реакция врага синхронны с уроном.

### Регрессии

- HUD и пауза;
- возврат в меню;
- чекпоинты;
- соло и кооп;
- компиляция без ошибок;
- отсутствие ошибок в Console/Player.log.

## 6. Критерии приёмки

- управление быстрее и предсказуемее, но не «ледяное»;
- приземление и разворот читаются визуально;
- каждый удар имеет различимую подготовку, контакт и завершение;
- враг физически реагирует на силу/направление, не хаотично;
- игрок получает i-frames и не прерывается бесконечно;
- хитбокс совпадает с визуальным моментом удара;
- эффекты не закрывают персонажей;
- кооператив не ухудшается;
- нет дублирующих систем и ошибок компиляции.

## 7. Порядок выполнения

1. Character Feel: `HamsterMovement` + `CharacterMotionVisual`.
2. Настройка префабов героев, проверка обоих персонажей.
3. Combat Feel: `HitData`/`DamageInfo`/`Hitbox`.
4. `HitReaction` и i-frames.
5. `AttackController`/timings/momentum.
6. VFX и landing feedback.
7. Play Mode-проверка и сборка.
## 13. Результат реализации (2026-11-13)

Реализовано:

- `HamsterMovement`:
  - плавный разгон, торможение и отдельное ускорение разворота;
  - coyote time, jump buffer, variable jump height;
  - усиленная гравитация падения и ограничение скорости падения;
  - события `Jumped`, `Landed`, `StartedMoving`, `StoppedMoving`, `DirectionChanged`;
  - отдельная блокировка прыжка в hurt/downed/death;
  - сохранение физического отброса во время hit reaction.
- `CharacterMotionVisual`:
  - процедурный наклон корпуса по скорости и направлению;
  - squash/stretch при приземлении;
  - пыль при приземлении на основе существующего `HitSpark`;
  - слабая тряска камеры при сильном приземлении.
- Combat Feel:
  - `HitData` расширен весом удара, цветом/длительностью вспышки, параметрами искр, камеры и наклона цели;
  - `DamageInfo` передаёт эти параметры в единый конвейер урона;
  - `Hitbox` передаёт фактическую точку контакта, варьирует искры и shake;
  - `HitReaction` реализует Light / Medium / Heavy / Launch, контролируемый наклон, полёт, приземление и recovery;
  - у игроков добавлены i-frames, мерцание неуязвимости и `HitReaction`;
  - AI больше не обнуляет скорость отброса во время реакции;
  - тайминги комбо приведены к длинам клипов, добавлен attack momentum.
- `HitSpark` поддерживает цвет и интенсивность.
- `FeelSystemsSetup` настраивает prefab-ы и HitData ассеты повторяемо.

Проверки:

- Unity batchmode compilation: `exit 0`, ошибок CS нет;
- standalone smoke test: PASS по landing, i-frames, light recoil, launch state и сохранению launch velocity после FixedUpdate;
- Windows build: `D:\Build\HamsterAdventures_SliceLatest\HamsterAdventures_SliceLatest.exe`;
- запуск собранной игры: стартует без runtime-исключений.

Ограничения:

- звуковые ресурсы в проекте отсутствуют (`Assets/Audio` пуста), поэтому звук не добавлялся и не подменялся случайными клипами;
- требуется пользовательская Play Mode-проверка визуального ощущения на обоих героях и в кооперативе;
- визуальная оценка силы squash/lean и читаемости новых impact-профилей требует ручного плейтеста.

### Исправления после плейтеста

- Устранён накопительный рост врага от вспышки: `HitReaction.Flash` использует отдельный `flashBaseScale`, а reaction-профили больше не перезаписывают базовый масштаб во время вспышки.
- Исправлен урон гиен из `ArenaRoot`: `Hitbox` определяет владельца через `GetComponentInParent<Health>()`, а не через `transform.root`. Это важно для prefab-инстансов, вложенных в общий контейнер арены.
- Проверено в standalone-сборке: AI обычной гиены наносит урон игроку, ручной хитбокс также наносит урон; пять ударов подряд не увеличивают масштаб врага.
