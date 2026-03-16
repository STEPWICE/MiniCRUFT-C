# MiniCRUFT

MiniCRUFT — экспериментальная воксельная sandbox‑игра в духе Minecraft Beta 1.7.3.
Проект написан на C# (.NET 10) и использует Veldrid (D3D11 по умолчанию).

**Ключевые особенности**
- процедурная генерация мира по seed
- высота мира 256 блоков
- биомы, реки, пещеры
- day/night и базовая погода
- разрушение и установка блоков
- базовые звуки блоков (ломание/установка)
- greedy meshing и frustum culling
- сохранение мира по чанкам
- RU/EN локализация

**Минимальные требования**
- CPU: Intel Core i5-2400
- GPU: NVIDIA GTX 650
- RAM: 8 GB
- ОС: Windows 10 / 11

**Быстрый запуск**
1. Установите .NET 10 SDK.
2. Запустите `run.bat`.

**Логи**
- `logs\run.log` — общий вывод запуска.
- `logs\engine.log` — внутренний лог движка.

**Управление**
- `W A S D` — движение
- `SPACE` — прыжок
- `ЛКМ` — разрушить блок
- `ПКМ` — поставить блок
- `ESC` — выход

**Структура проекта**
- `assets` — все игровые ресурсы
- `docs` — документация
- `src` — исходный код (solution + проекты)
- `world` — сохранения
- `run.bat` — запуск проекта

**Конфигурация**
`config.json` в корне проекта:
- `ChunkRadius`
- `VSync`
- `Fullscreen`
- `WindowWidth`, `WindowHeight`
- `Seed`
- `AssetsPath`
- `WorldPath`
- `Language`
- `FieldOfView`
- `MouseSensitivity`
- `PlayerSpeed`
- `Render.FogStart`, `Render.FogEnd`
- `Render.BiomeTintStrength`, `Render.MinLight`, `Render.SunLightMin`
- `Render.Foliage.TintStrength`, `Render.Foliage.CutoutAlphaThreshold`
- `Render.Foliage.DitherStrength`, `Render.Foliage.DitherScale`
- `Atmosphere.CloudRadius`, `Atmosphere.CloudCellSize`
- `Atmosphere.CloudOpacity`, `Atmosphere.CloudTiling`, `Atmosphere.CloudSpeed`
- `Atmosphere.WaterTint`, `Atmosphere.WaterOpacity`
- `Ui.HudScale`, `Ui.HotbarYOffset`, `Ui.HeartsYOffset`, `Ui.HeartsXOffset`
- `Physics.PlayerWidth`, `Physics.PlayerHeight`, `Physics.EyeHeight`, `Physics.Gravity`, `Physics.JumpVelocity`, `Physics.SprintMultiplier`
- `DayNight.DayLengthSeconds`, `DayNight.StartTimeOfDay`, `DayNight.MinSunIntensity`
- `Weather.ToggleIntervalSeconds`, `Weather.ToggleChance`
- `Audio.Enabled`, `Audio.MaxActive`, `Audio.DigVolume`, `Audio.PlaceVolume`
- `Save.EnableAutoSave`, `Save.AutoSaveIntervalSeconds`, `Save.SaveWorkers`, `Save.UnloadExtraRadius`

**Ассеты**
Используются ресурсы из `assets\minecraft`:
- `textures\block` — текстуры блоков
- `textures\water` — вода
- `font` — TTF
- `sounds` и `music` — аудио
- `lang_ru.json`, `lang_en.json` — локализация

**Сейвы**
Мир хранится по чанкам:
- `world\seed.dat`
- `world\player.dat`
- `world\chunks\chunk_X_Z.dat`

**Оптимизация**
- greedy meshing
- frustum culling
- асинхронная генерация чанков
- очередь rebuild мешей
- линейный цветовой пайплайн (sRGB↔Linear)

**Лицензия**
Только некоммерческое использование.
