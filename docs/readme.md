# MiniCRUFT

MiniCRUFT — экспериментальная воксельная sandbox-игра в духе Minecraft Beta 1.7.3.
Проект написан на C# (.NET 10) и использует Veldrid (D3D11 по умолчанию).

## Что уже есть
- процедурная генерация мира по `Seed`
- биомы, реки, пещеры, поверхности и декоративные микрофичи
- подземные aquifer-полосы воды и лавы
- day/night, погода, облака, небо и редкие storm-события
- установка и разрушение блоков
- физика падения песка и гравия
- физика воды и лавы с уровнями, источниками и затвердеванием при контакте
- звук разрушения, установки, шага, бега и прыжка с привязкой к типу поверхности
- мобы: спавн, ИИ, физика движения, бой, взрывы криперов, пассивный flee, обход препятствий, звук и сохранение
- survival-loop: голод, еда, отдых, крафт, инструменты с прочностью, переплавка, расход ресурсов и дропы с мобов/сундуков
- точки интереса: лагеря, башни, руины, шахты и редкие сундуки с лутом
- частицы от разрушения блоков, установки, шага и прыжка
- greedy meshing, frustum culling и LOD-даль
- сохранение мира по чанкам и автосейв
- RU/EN локализация
- HUD, хотбар, инвентарь и иконки блоков
- единый TTF-шрифт для UI

## Быстрый запуск
1. Установите .NET 10 SDK.
2. Запустите `run.bat`.

## Минимальные требования
- CPU: Intel Core i5-2400
- GPU: NVIDIA GTX 650
- RAM: 8 GB
- ОС: Windows 10 / 11

## Управление
- `W A S D` - движение
- `SPACE` - прыжок
- `ЛКМ` - разрушить блок
- `ПКМ` - поставить блок
- `ПКМ` по еде - съесть предмет и восстановить голод
- `E` - инвентарь
- `C` - крафт следующего доступного рецепта в инвентаре
- `V` - переплавка следующего доступного рецепта в инвентаре
- `R` - отдых до утра, если безопасно и уже темно
- `ПКМ` по сундуку - открыть его и забрать лут
- `F3` - отладочный HUD
- `F5` - меню выбора биома
- `F6` - меню seed
- `Enter` - применить выбор или перегенерировать мир
- `F11` - полноэкранный режим
- `ESC` - закрыть меню или выйти

## Структура проекта
- `assets` - игровые ресурсы
- `docs` - документация
- `src` - исходный код
- `world` - локальные сохранения мира
- `run.bat` - запуск

## Конфигурация
`config.json` в корне проекта:

- `ChunkRadius`, `ChunkPreloadExtra`
- `VSync`, `Fullscreen`, `WindowX`, `WindowY`, `WindowWidth`, `WindowHeight`
- `WindowX`/`WindowY` хранят последнюю оконную позицию, а `WindowWidth`/`WindowHeight` - последний оконный размер, который игра восстановит при следующем запуске.
- Если `config.json` создаётся впервые, окно стартует по центру текущего экрана.
- `Seed`, `Language`, `AssetsPath`, `WorldPath`
- `FieldOfView`, `MouseSensitivity`, `PlayerSpeed`
- `ShowDebug`, `ResetWorldOnLaunch`
- `Physics.PlayerWidth`, `Physics.PlayerHeight`, `Physics.EyeHeight`, `Physics.Gravity`, `Physics.JumpVelocity`, `Physics.SprintMultiplier`
- `DayNight.DayLengthSeconds`, `DayNight.StartTimeOfDay`, `DayNight.MinSunIntensity`
- `Weather.ToggleIntervalSeconds`, `Weather.ToggleChance`, `Weather.RainDarkenR`, `Weather.RainDarkenG`, `Weather.RainDarkenB`, `Weather.LightningChancePerSecond`, `Weather.LightningMinRainIntensity`, `Weather.LightningFlashFadeSeconds`, `Weather.LightningFlashStrength`, `Weather.LightningThunderDelaySeconds`, `Weather.LightningCooldownSeconds`
- `Audio.Enabled`, `Audio.MaxActive`, `Audio.MasterVolume`, `Audio.DigVolume`, `Audio.PlaceVolume`, `Audio.AmbientVolume`, `Audio.MusicVolume`, `Audio.LiquidVolume`, `Audio.FireVolume`, `Audio.StepVolume`, `Audio.RunVolume`, `Audio.JumpVolume`
- `Audio.StepDistance`, `Audio.RunStepDistance`, `Audio.AmbientIntervalSeconds`, `Audio.MusicIntervalSeconds`, `Audio.LiquidIntervalSeconds`, `Audio.LiquidRadius`
- `Particles.Enabled`, `Particles.MaxParticles`, `Particles.Gravity`, `Particles.Drag`, `Particles.BlockBreakCount`, `Particles.BlockPlaceCount`, `Particles.StepCount`, `Particles.JumpCount`, `Particles.BlockBreakLifetime`, `Particles.BlockPlaceLifetime`, `Particles.StepLifetime`, `Particles.JumpLifetime`, `Particles.BlockBreakSize`, `Particles.BlockPlaceSize`, `Particles.StepSize`, `Particles.JumpSize`, `Particles.BlockBreakSpeed`, `Particles.BlockPlaceSpeed`, `Particles.StepSpeed`, `Particles.JumpSpeed`, `Particles.StepUpwardBias`, `Particles.JumpUpwardBias`
- `Save.EnableAutoSave`, `Save.AutoSaveIntervalSeconds`, `Save.SaveWorkers`, `Save.UnloadExtraRadius`
- `Spawn.Mode`, `Spawn.SearchRadius`, `Spawn.MaxAttempts`, `Spawn.MinHeightAboveSea`, `Spawn.MaxSlope`, `Spawn.Randomize`, `Spawn.ExcludedBiomes`
- `Falling.Enabled`, `Falling.MaxUpdatesPerFrame`
- `Fluid.Enabled`, `Fluid.MaxUpdatesPerFrame`, `Fluid.WaterMaxSpreadLevel`, `Fluid.LavaMaxSpreadLevel`, `Fluid.LavaUpdatesPerFrame`, `Fluid.LavaUpdateIntervalFrames`, `Fluid.InfiniteSources`, `Fluid.LavaInfiniteSources`, `Fluid.ReplaceNonSolid`, `Fluid.LavaHardensOnWaterContact`
- `Mob.Enabled`, `Mob.MaxAlive`, `Mob.SpawnRadius`, `Mob.DespawnRadius`, `Mob.SpawnIntervalSeconds`, `Mob.SpawnAttemptsPerTick`, `Mob.PlayerAttackDamage`, `Mob.PlayerAttackCooldownSeconds`, `Mob.PlayerDamageCooldownSeconds`, `Mob.HostileDayMultiplier`, `Mob.HostileNightMultiplier`, `Mob.PassiveDayMultiplier`, `Mob.PassiveNightMultiplier`, `Mob.Gravity`, `Mob.WaterSlowMultiplier`, `Mob.WaterBuoyancy`, `Mob.LavaDamagePerSecond`, `Mob.EdgeAvoidDistance`, `Mob.StepHeight`, `Mob.JumpVelocity`, `Mob.WanderChangeSeconds`, `Mob.ZombieWeight`, `Mob.CreeperWeight`, `Mob.CowWeight`, `Mob.SheepWeight`, `Mob.ChickenWeight`
- `Render.*` - туман, освещение, палитры, mipmaps и LOD
- `Atmosphere.*` - облака, небо, вода и лава
- `Ui.*` - масштаб HUD, инвентарь, текст, шрифт
- `WorldGen.*` - параметры генерации рельефа, биомов, рек, пещер, aquifer-источников воды и лавы, деревьев, декора и редких POI

## Ассеты
Используются ресурсы из `assets\minecraft`:

- `textures\block` - текстуры блоков
- `textures\entity` - текстуры мобов
- `textures\water` - вода
- `textures\colorMap` - colormap травы и листвы
- `font` - TTF-шрифт UI
- `sounds\mob` - звуки мобов
- `sounds` и `music` - аудио
- `lang_ru.json`, `lang_en.json` - локализация

## Сейвы
Мир хранится по чанкам:

- `world\seed.dat`
- `world\player.dat`
- `world\mobs.dat`
- `world\chunks\chunk_X_Z.dat`

Поддерживаемые форматы:

- `seed.dat` читается как текстовый `int` и как legacy binary `Int32`
- `player.dat` поддерживает текущий формат `v2` и legacy формат только с позицией
- `mobs.dat` хранит состояние мобов и сериализуется через `MobSaveData`
- `chunk_X_Z.dat` поддерживает версии `1` и `2`; более старые версии отвергаются безопасно

## Оптимизация
- greedy meshing
- frustum culling
- асинхронная генерация чанков
- очередь rebuild мешей
- система частиц для игрового feedback
- линейный цветовой пайплайн `sRGB` ↔ `Linear`
- отдельные очереди для сохранения и физики жидкости

## Тесты
Автопокрытие сейчас проверяет:

- roundtrip и normalisation конфигурации
- fallback для частичного и битого `config.json`
- детерминизм генерации мира для фиксированного seed
- smoke-пути сохранения seed, player и chunk
- smoke-путь сохранения и загрузки mobs
- mob steering и flee-behavior
- legacy-совместимость для `player.dat`, `seed.dat` и chunk-версий `1`/`2`

## Лицензия
Только некоммерческое использование.
