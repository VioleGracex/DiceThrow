# 🎲 BG3 Dice System — Unity Test Assignment / Тестовое Задание Unity

[English](#english) | [Русский](#русский)

---

## English

### Overview
A Baldur's Gate 3 style 3D dice throwing mechanic built in Unity. Features physics simulation, face detection, dynamic skill checks (DC), modifier card calculations, and sound/visual feedback.

### Key Features
* **3D Physics Dice Throwing:** Dynamic impulse and torque throwing with circular arena boundary bounce.
* **Precise Face Detection:** Quaternion and vector-based face orientation detection for D4, D6, D8, D10, D12, and D20 dice.
* **Skill Check & Modifiers:** Support for DC thresholds, Natural 20 (Critical Success), Natural 1 (Critical Fail), and sequential modifier additions.
* **Roll Modes:** Single Die and Advantage (2 Dice — pick highest).
* **Rich UI & Feedback:** BG3-inspired interface, smooth tweens, particle effects, and audio feedback.

### Assets Used
* **UI & Graphics:** Generated via **Gemini AI**.
* **Audio & Sound FX:** Generated via **ElevenLabs**.
* **3D Dice Models:** Sourced from **Sketchfab**.

### How to Run
1. Open the project in **Unity 2022.3+** (or Unity 6).
2. Open scene `Assets/_Game/Scenes/MainScene.unity`.
3. Press **Play** in Editor or launch the compiled `.exe` build.
4. Click directly on the 3D Die or click the **Roll** button.

---

## Русский

### Описание
Механика 3D броска кубика в стиле Baldur's Gate 3, разработанная на Unity. Включает физику броска, детекцию выпавшей грани, расчет проверок навыков (DC), учет модификаторов и визуально-звуковой фидбек.

### Основные Возможности
* **Физика 3D Броска:** Запуск кубика с импульсом и кручением, удерживание в границах арены с реалистичным отскоком.
* **Точная Детекция Граней:** Определение выпавшего значения по вектору нормали и углам поворота для кубиков D4, D6, D8, D10, D12, D20.
* **Проверки Навыков и Модификаторы:** Расчет Класса Сложности (DC), авто-победа при Natural 20, авто-поражение при Natural 1, пошаговое суммирование бонусов.
* **Режимы Броска:** Единичный бросок и бросок с Преимуществом (2 кубика — выбор наивысшего).
* **UI и Сочный Фидбек:** Интерфейс в стиле BG3, программные анимации твининга, партиклы и сочные звуковые эффекты.

### Использованные Ассеты
* **UI и Графика:** Сгенерировано через **Gemini AI**.
* **Звуки и Озвучка:** Сгенерировано через **ElevenLabs**.
* **3D-Модели Кубиков:** Модели с **Sketchfab**.

### Запуск Проекта
1. Откройте проект в **Unity 2022.3+** (или Unity 6).
2. Откройте сцену `Assets/_Game/Scenes/MainScene.unity`.
3. Нажмите **Play** в редакторе или запустите собранный `.exe`.
4. Кликните по 3D кубику на экране или нажмите кнопку **Roll** ("Бросок").

---

### 📄 Technical Documentation / Документация
Detailed C# technical code documentation with tabulated snippets and Russian comments:  
[TECHNICAL_IMPLEMENTATION_RU.md](TECHNICAL_IMPLEMENTATION_RU.md)
