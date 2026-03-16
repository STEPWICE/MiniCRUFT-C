# ARCHITECTURE.md

**Обзор**
MiniCRUFT — модульный воксельный движок на C# (.NET 10) с рендером через Veldrid (D3D11 по умолчанию). Архитектура разбита на независимые модули, чтобы упростить развитие и минимизировать связанность.

**Модули**
- `MiniCRUFT.Core` — базовые типы, конфиг, логирование
- `MiniCRUFT.World` — блоки, чанки, генерация, освещение
- `MiniCRUFT.Renderer` — GPU‑рендер, меши, атлас, UI
- `MiniCRUFT.Game` — игровой цикл, игрок, ввод, логика
- `MiniCRUFT.IO` — хранение чанков и сохранений
- `MiniCRUFT.UI` — HUD и локализация
- `MiniCRUFT.Audio` — аудио‑бэкенды (NAudio / no‑op)

**Core**
- `GameConfig` — загрузка и хранение настроек
- `AssetStore` — единая система ассетов и кэш
- `RenderConfig` / `AtmosphereConfig` / `UiConfig` / `WorldGenConfig` — параметры подсистем
- `Log` — единый лог движка
- `ColorSpace` — конвертация sRGB ↔ Linear для единого цветового пайплайна
- `Vector3i`, `ChunkCoord`, `BlockCoord` — целочисленные координаты
- `NoiseService` — обёртка над шумами генерации

**World**
- `Chunk` — данные чанка 16×16×256 (блоки + lightmap)
- `BlockRegistry` / `BlockDefinition` — свойства блоков и текстуры
- `BiomeRegistry` — биомы и их параметры
- `BiomeColorMap` — colormap травы/листвы
- `WorldGenerator` — генерация terrain/biomes/rivers/caves/vegetation
- `WorldLighting` — sunlight + torchlight, flood fill
- `TreeGenerator` — генерация деревьев
- `World` — доступ к чанкам и блокам

**Renderer**
- `RenderDevice` — окно, swapchain, командный буфер
- `WorldRenderer` — рендер чанков, frustum culling
- `ChunkMeshBuilder` — greedy meshing и освещение
- `TextureAtlas` — атлас текстур блоков
- `ChunkMesh` — GPU‑меши (solid/transparent)
- `SkyRenderer` — градиентное небо, солнце/луна
- `CloudRenderer` — 3D‑облака (мировой слой)
- `SpriteAtlas` / `SpriteBatch` — спрайтовый рендер (HUD/sky)
- `UiRenderer` / `UiTextRenderer` — HUD и текст

`RenderDevice` пытается запуститься на D3D11, при ошибке включает fallback на Vulkan или OpenGL.

**Game**
- `GameApp` — главный цикл и инициализация систем
- `Player`, `Camera`, `InputState`, `InputHandler` — управление
- `Inventory` — хотбар
- `WorldRaycaster` — выбор блока
- `ChunkManager` — загрузка/выгрузка/mesh rebuild
- `DayNightCycle`, `WeatherSystem` — атмосфера
- `SoundSystem` — базовые звуки блоков

**IO**
- `FileChunkStorage` — сохранение чанков на диск
- `WorldSave` — seed и состояние игрока
- `ChunkSaveQueue` — очередь фонового сохранения чанков

**UI**
- `HudState` — состояние HUD
- `Localization` — строки RU/EN

**Audio**
- `IAudioBackend` — интерфейс аудио‑бэкенда
- `NaudioBackend` — реализация для Windows
- `NoAudioBackend` — заглушка для других ОС

**World Events**
- `WorldChangeQueue` — очередь изменений мира
- `WorldEditor` — единая точка изменения блоков

**Потоки**
- Main thread: ввод, логика, рендер
- Chunk generation workers: генерация чанков
- Mesh build workers: greedy meshing

**Игровой цикл**
1. Ввод и обновление игрока
2. Обновление мира и очередей генерации
3. Перестройка мешей при изменениях
4. Рендер неба, чанков, 3D‑облаков и HUD

**Оптимизация**
- greedy meshing
- frustum culling
- асинхронная генерация чанков
- очередь rebuild
- texture atlas
