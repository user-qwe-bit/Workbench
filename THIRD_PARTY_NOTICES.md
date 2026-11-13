# Авторы и лицензии

- **m6x11cyrillic — Daniel Linssen and Natto Games.** Предоставленный пользователем файл, свободное использование с указанием авторов. [Страница шрифта](https://nattogames.itch.io/m6x11cyrillic), [оригинальная семья](https://managore.itch.io/m6x11). Исходное уведомление сохранено в `kek/pack/Docs/CREDITS.md`. Не заявляется лицензия SIL OFL.
- **Графика RobotArenaArt v4:** предоставленные ресурсы пользователя, созданные для этого проекта встроенным image_gen и подготовленные в исходном пакете. Исходники, референсы и история сохранены в `kek`. Перегенерация графики не выполнялась.
- **Звуки Workbench:** оригинальный программный синтез пластиковых щелчков, пружинных тонов и коротких шумовых вспышек в `Tools/prepare_content.py`. Внешние звукозаписи не использованы.
- **CPython 3.12.10:** Python Software Foundation License; полный `LICENSE.txt` включён в `Assets/StreamingAssets/InputRuntime/python`.
- **MediaPipe 0.10.32 и модель Hand Landmarker:** Google / MediaPipe Authors, Apache 2.0. [Проект](https://github.com/google-ai-edge/mediapipe), [модель и документация](https://ai.google.dev/edge/mediapipe/solutions/vision/hand_landmarker). Лицензия wheel находится в `python/Lib/site-packages/mediapipe-0.10.32.dist-info/licenses`.
- **Vosk / Kaldi:** Alpha Cephei и авторы Kaldi, Apache 2.0. [Vosk](https://github.com/alphacep/vosk-api), [Kaldi](https://github.com/kaldi-asr/kaldi). Русская модель `vosk-model-small-ru-0.22`, Apache 2.0 по [каталогу моделей](https://alphacephei.com/vosk/models). Включены модель и нативная библиотека Windows x64.
- **OpenCV:** OpenCV team, Apache 2.0. **NumPy:** NumPy developers, BSD. **sounddevice / PortAudio:** MIT. **CFFI:** MIT. Точные версии, метаданные и лицензии транзитивных пакетов сохранены в каталогах `python/Lib/site-packages/*.dist-info`.
- **MinGW/GCC runtime** (`libgcc_s_seh-1.dll`, `libstdc++-6.dll`, `libwinpthread-1.dll`): соответствующие авторы GCC и mingw-w64; GPL с GCC Runtime Library Exception для GCC runtime, лицензии mingw-w64 для winpthreads. [Runtime exception](https://www.gnu.org/licenses/gcc-exception-3.1.html), [mingw-w64](https://www.mingw-w64.org/).

Компоненты распознавания скопированы из уже существовавших локальных redistributable-комплектов соседних проектов; игровой код этих проектов не переносился. Версии и хеш модели записаны в `Assets/StreamingAssets/InputRuntime/provenance.json`. Все лицензии в зависимостях должны сохраняться при упаковке Windows-сборки.

Дополнительный предоставленный пользователем набор `huh` (коннекторы, шкалы, переходы, загрузка, поверхности) сохранён в исходном виде; инвентаризация в `TestResults/HuhAudit`. Отдельной лицензии внутри набора нет. Использован как предоставленный контент этого проекта.
