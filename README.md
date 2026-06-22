# MacroRecoReplayer

A lightweight Windows utility for recording and replaying mouse and keyboard macros. Runs in the system tray.

## Features
- **Global Hotkeys:** Control recording and playback without switching windows.
- **Smart Recording:** Automatically merges fast clicks and key presses (under 200ms) into single actions. Filters out key auto-repeats.
- **Mouse Move Recording:** Single press of `Ctrl` during recording saves the current mouse position as a smooth `move` command.
- **Repeat Mode:** Support for `repeat x` to run the macro a specific number of times.
- **Smooth Playback:** Mouse movements are interpolated for natural-looking transitions.
- **Safety Release:** Automatically releases all stuck keyboard and mouse buttons when playback finishes or is interrupted.
- **System Tray Integration:** Select active scripts, open the scripts folder, and manage the app via the tray icon.
- **Infinite Loop:** Support for continuous macro execution.

## Hotkeys
- `Alt + R` — Start recording (with a 0.5s delay).
- `Alt + Shift + R` — Stop recording.
- `Alt + P` — Start playback of the selected script.
- `Alt + Shift + P` — Stop playback.
- `Ctrl` (single press) — Record smooth mouse movement to current position (`move`).

## Script Format (`.recore`)
Scripts are saved as plain text files with the `.recore` extension. 
The first line can define the execution mode:
- `loop` — Infinite loop.
- `repeat x` — Repeat the macro `x` times (if `x <= 0`, it defaults to 1).
- *(Empty/Other)* — Single execution.

Each subsequent line represents an action with a delay in seconds:
`delay action [parameters]`

Examples:
- `0.50 click_l 500 300`
- `0.10 key a`
- `1.00 mouse_l_down 100 100`
- `0.50 move 800 600` (Smooth mouse movement to coordinates)

## Requirements & Building
- **OS:** Windows 7 or newer.
- **Framework:** .NET Framework 4.7.1 or 4.8.
- Build using Visual Studio (Windows Forms App). The debug console is only visible when compiled in `Debug` mode.

---

# MacroRecoReplayer

Легковесная утилита для Windows для записи и воспроизведения макросов мыши и клавиатуры. Работает в системном трее.

## Возможности
- **Глобальные хоткеи:** Управление записью и воспроизведением без переключения окон.
- **Умная запись:** Автоматически объединяет быстрые клики и нажатия клавиш (менее 200 мс) в одиночные действия. Фильтрует автоповтор клавиш.
- **Запись движения мыши:** Одиночное нажатие `Ctrl` во время записи сохраняет текущую позицию мыши как команду плавного движения `move`.
- **Режим повтора:** Поддержка директивы `repeat x` для выполнения макроса заданное количество раз.
- **Плавное воспроизведение:** Движения мыши интерполируются для естественных переходов.
- **Безопасный сброс:** Автоматически отпускает все «залипшие» клавиши клавиатуры и кнопки мыши при завершении или прерывании воспроизведения.
- **Интеграция в трей:** Выбор активного скрипта, открытие папки со скриптами и управление приложением через иконку в трее.
- **Бесконечный цикл:** Поддержка непрерывного выполнения макроса.

## Хоткеи
- `Alt + R` — Начать запись (с задержкой 0.5 с).
- `Alt + Shift + R` — Остановить запись.
- `Alt + P` — Начать воспроизведение выбранного скрипта.
- `Alt + Shift + P` — Остановить воспроизведение.
- `Ctrl` (одиночное нажатие) — Записать плавное движение мыши в текущую позицию (`move`).

## Формат скриптов (`.recore`)
Скрипты сохраняются как обычные текстовые файлы с расширением `.recore`.
Первая строка может задавать режим выполнения:
- `loop` — Бесконечный цикл.
- `repeat x` — Повторить макрос `x` раз (если `x <= 0`, выполняется 1 раз).
- *(Пусто/Другое)* — Однократное выполнение.

Каждая последующая строка представляет собой действие с задержкой в секундах:
`задержка действие [параметры]`

Примеры:
- `0.50 click_l 500 300`
- `0.10 key a`
- `1.00 mouse_l_down 100 100`
- `0.50 move 800 600` (Плавное движение мыши к координатам)

## Требования и сборка
- **ОС:** Windows 7 или новее.
- **Фреймворк:** .NET Framework 4.7.1 или 4.8.
- Сборка в Visual Studio (Windows Forms App). Консоль отладки видна только при компиляции в режиме `Debug`.