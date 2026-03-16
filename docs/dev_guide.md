# DEV_GUIDE.md

**Сборка**
```
dotnet build src\MiniCRUFT.sln -c Release
```

**Запуск**
```
run.bat
```

**Логи**
- `logs\run.log` — консольный вывод
- `logs\engine.log` — внутренние события и ошибки

**Добавление нового блока**
1. Обновите `BlockId`.
2. Зарегистрируйте блок в `BlockRegistry`.
3. Добавьте текстуры в `assets\minecraft\textures\block`.
4. Убедитесь, что текстуры 16×16 (атлас масштабирует, но лучше исходный размер 16×16).

**Добавление нового биома**
1. Добавьте биом в `BiomeRegistry`.
2. Подключите биом в `WorldGenerator` (логика выбора или распределения).

**Добавление дерева**
1. Реализуйте генерацию в `TreeGenerator`.
2. Вызовите генерацию в `WorldGenerator` для нужных биомов.

**Добавление локализации**
1. Обновите `assets\lang_ru.json` и `assets\lang_en.json`.
2. Добавьте новые ключи и используйте их через `Localization`.

**Звуки**
- Базовые звуки берутся из `assets\minecraft\sounds\dig` и `assets\minecraft\sounds\step`.
- Для новых материалов добавьте наборы `*_1..4.ogg` и убедитесь, что имя содержит ключ (grass, stone, sand, gravel, wood, snow).

**Настройка генерации**
Параметры находятся в `WorldGenSettings`:
- `SeaLevel`, амплитуды шумов, пороги рек и пещер.

**Настройка клиента**
Файл `config.json`:
- `ChunkRadius`, `Seed`, `Language`, `FieldOfView` и др.
- `Atmosphere.CloudRadius`, `Atmosphere.CloudCellSize` — размер 3D‑облаков
- `Render.Foliage.DitherStrength`, `Render.Foliage.DitherScale` — сглаживание cutout‑листвы
- `Render.FogStart`, `Render.FogEnd` — дальность тумана
- `Physics.*` — параметры движения игрока
- `DayNight.*`, `Weather.*` — цикл дня и погода
- `Audio.*` — параметры звука
- `Save.*` — автосейв и выгрузка чанков

**Профилирование**
Следите за:
- временем генерации чанков
- временем перестройки мешей
- количеством чанков в радиусе

**Цветовой пайплайн**
- Все расчёты выполняются в Linear, вывод — в sRGB (гамма‑коррекция).

**Стиль кода**
- `PascalCase` для типов и публичных методов
- `camelCase` для локальных переменных
- один класс — один файл
