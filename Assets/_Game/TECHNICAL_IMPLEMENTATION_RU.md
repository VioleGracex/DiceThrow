# Техническая Документация: Система Броска Кубиков (BG3 Dice System)
**Язык исполнения:** C# (Unity 2022.3+ / 6000+)  
**Назначение документа:** Беглая оценка технической реализации ключевых алгоритмов, архитектурных решений и использованных ресурсов.

---

## 1. Использованные Ресурсы (Used Assets)

Для создания эстетичного и погружающего пользовательского опыта в проекте были использованы следующие ресурсы и генеративные технологии:

*   **Интерфейс пользователя (UI & Graphics):**
    *   **Источник:** Сгенерировано с помощью **AI Gemini** (Google Gemini).
    *   **Описание:** Текстуры рамки броска, плашки модификаторов, иконки характеристик, стилизованные фон-карточки и элементы прогресс-бара.
*   **Звуковые эффекты и Озвучка (Sound & Audio FX):**
    *   **Источник:** Сгенерировано в **ElevenLabs**.
    *   **Описание:** Звуки шуршания и столкновения кубиков, тяжелый падение на стол, торжественный аккорд при критическом успехе (Critical Success), а также драматический звук при неудаче (Critical Fail).
*   **3D-Модели Кубиков (3D Dice Models):**
    *   **Источник:** Загружены с **Sketchfab**.
    *   **Описание:** Высокодетализированные полигональные 3D-модели многогранников: D4, D6, D8, D10, D12 и D20 с оптимизированными коллайдерами и запеченными текстурами материалов.

---

## 2. Ключевые Алгоритмы и Исходный Код C# (с Комментариями)

Ниже представлены ключевые алгоритмические модули системы с четкой табуляцией и развернутыми комментариями C# на русском языке.

---

### Алгоритм 1: Определение Выпавшей Грани Кубика (`DiceResultDetector.cs`)

**Описание:** Алгоритм использует таблицу кватернионов/углов Эйлера, предварительно сопоставленных с каждой гранью 3D-модели. Метод `GetUpwardFaceValue()` сопоставляет текущий поворот кубика в пространстве с углами граней и выбирает грань с минимальным угловым отклонением.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BG3DiceSystem.Gameplay.Dice
{
	/// <summary>
	/// Структура записи соответствия значения грани и её угла поворота Эйлера.
	/// </summary>
	[Serializable]
	public struct DiceFaceEntry
	{
		public int FaceValue;           // Значение на грани (например, 1..20)
		public Vector3 EulerRotation;   // Угол Эйлера, при котором эта грань направлена на камеру
	}

	[ExecuteAlways]
	public class DiceResultDetector : MonoBehaviour
	{
		[Header("Конфигурация Кубика")]
		public DiceType Type = DiceType.D20;

		[Header("Таблица поворотов граней")]
		public List<DiceFaceEntry> FaceRotations = new List<DiceFaceEntry>();

		/// <summary>
		/// Возвращает значение грани, которая в данный момент наиболее точно направлена на камеру.
		/// </summary>
		/// <returns>Целое число - значение грани</returns>
		public int GetUpwardFaceValue()
		{
			// Проверка на инициализацию таблицы граней
			if (FaceRotations == null || FaceRotations.Count == 0) 
			{
				ResetToDefaults();
			}

			float minAngle = float.MaxValue;
			int bestValue = 1;

			// Проходим по всем известным граням кубика
			foreach (var entry in FaceRotations)
			{
				// Вычисляем угловую разницу между текущим поворотом кубика и целевым поворотом грани
				Quaternion targetRot = Quaternion.Euler(entry.EulerRotation);
				float angle = Quaternion.Angle(transform.rotation, targetRot);

				// Находим грань с минимальным угловым отклонением (наиболее близкую)
				if (angle < minAngle)
				{
					minAngle = angle;
					bestValue = entry.FaceValue;
				}
			}

			return bestValue;
		}

		/// <summary>
		/// Возвращает кватернион поворота для визуального выравнивания грани ровно на камеру.
		/// </summary>
		public Quaternion GetFacingRotation(int faceValue, Vector3 cameraDir)
		{
			if (FaceRotations == null || FaceRotations.Count == 0) 
			{
				ResetToDefaults();
			}

			foreach (var entry in FaceRotations)
			{
				if (entry.FaceValue == faceValue)
				{
					return Quaternion.Euler(entry.EulerRotation);
				}
			}
			return Quaternion.identity;
		}
	}
}
```

---

### Алгоритм 2: Детекция Интерактивного Клика по 3D Кубику (`DiceDirectRaycastDetector.cs`)

**Описание:** Алгоритм пускает Raycast из 오버лейной (Overlay) камеры Unity через позицию курсора/тапа. При попадании по мешу 3D-кубика проверяются условия (блокировка UI, кулдаун, состояние броска) и инициируется бросок.

```csharp
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BG3DiceSystem.UI
{
	public class DiceDirectRaycastDetector : MonoBehaviour
	{
		public event Action OnRollRequested;

		[Header("Настройки Raycast")]
		public Camera OverlayCamera;          // Камера рендеринга 3D кубиков
		public LayerMask DiceLayerMask = -1;  // Маска слоя 3D-кубиков
		public bool IsEnabled = true;
		public float CooldownDuration = 5.0f; // Защитный кулдаун от спама

		private float _cooldownTimer = 0f;

		private void Update()
		{
			// 1. Обновляем таймер перезарядки (Cooldown)
			if (_cooldownTimer > 0f)
			{
				_cooldownTimer -= Time.deltaTime;
				if (_cooldownTimer > 0f) return;
			}

			if (!IsEnabled) return;

			// 2. Если система уже выполняет бросок - игнорируем нажатия
			var uiController = UnityEngine.Object.FindFirstObjectByType<BG3DiceSystem.UI.UIController>();
			if (uiController != null && uiController.IsRolling) return;

			// 3. Защита: если курсор находится над элементами UI (кнопки, карточки) - не бросаем
			if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

			// 4. Получаем актуальный ввод от New Input System
			var pointer = Pointer.current;
			if (pointer == null) return;

			// Проверяем факт нажатия в текущем кадре
			if (!pointer.press.wasPressedThisFrame && !pointer.press.wasReleasedThisFrame) return;

			Vector2 screenPos = pointer.position.ReadValue();
			Ray ray = OverlayCamera.ScreenPointToRay(screenPos);

			// 5. Пускаем Raycast во все физические объекты слоя кубиков
			RaycastHit[] hits = Physics.RaycastAll(ray, 100f, DiceLayerMask);
			DiceController foundDie = null;

			foreach (var hit in hits)
			{
				if (hit.collider == null) continue;
				
				// Игнорируем невидимые физические стены арены
				if (hit.collider.gameObject.name.Contains("Wall")) continue;

				var dc = hit.collider.GetComponentInParent<DiceController>();
				if (dc != null && dc.gameObject.activeInHierarchy)
				{
					foundDie = dc;
					break;
				}
			}

			// 6. Если клик попал прямо по 3D-модели кубика — запускаем процесс броска
			if (foundDie != null)
			{
				TriggerCooldown(CooldownDuration);
				OnRollRequested?.Invoke(); // Генерируем событие старта броска
			}
		}

		public void TriggerCooldown(float duration = -1f)
		{
			_cooldownTimer = duration > 0f ? duration : CooldownDuration;
		}
	}
}
```

---

### Алгоритм 3: Расчет Итогового Результата и Проверка DC (`RollService.cs`)

**Описание:** Алгоритм обрабатывает результаты единичного броска или броска с преимуществом (Advantage), проверяет условия Критического Успеха (Natural Max / Nat 20) и Критической Неудачи (Natural 1), суммирует значения с модификатором и формирует итоговый объект `FinalRoll`.

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using BG3DiceSystem.Core.Interfaces;
using BG3DiceSystem.Gameplay.Roll;

namespace BG3DiceSystem.Core.Services
{
	public class RollService : IRollService
	{
		private readonly IDiceService _diceService;
		private readonly ISkillService _skillService;
		private readonly IAudioService _audioService;

		/// <summary>
		/// Основной асинхронный метод выполнения полного цикла броска.
		/// </summary>
		public async Task<FinalRoll> ExecuteRollAsync()
		{
			// 1. Асинхронно запускаем физический бросок кубиков и ждем их остановки
			List<int> diceValues = await _diceService.RollDiceAsync(CurrentRollMode);
			if (diceValues == null || diceValues.Count == 0) return default;

			int diceA = diceValues[0];
			int diceB = diceValues.Count > 1 ? diceValues[1] : diceA;

			// 2. Логика выбора значения при Преимуществе (Advantage) vs Обычном броске
			int selectedValue = (CurrentRollMode == RollMode.AdvantageTwoDice)
				? Mathf.Max(diceA, diceB)
				: diceA;

			int modifier = _skillService.CurrentModifier;
			int dc = _skillService.CurrentDC;
			int total = selectedValue + modifier;

			int maxDieValue = SkillService.GetMaxDieValue(_diceService.CurrentDiceType);

			// 3. Проверка особых условий (Nat 20 / Nat 1)
			bool isNatMax = (selectedValue == maxDieValue);
			bool isNat1 = (selectedValue == 1);
			bool isSuccess;

			if (isNatMax)
			{
				// Натуральный максимум (Nat 20) = ВСЕГДА Автоматический Критический Успех независимо от DC
				isSuccess = true;
			}
			else if (isNat1)
			{
				// Натуральная 1 = ВСЕГДА Автоматическая Критическая Неудача
				isSuccess = false;
			}
			else
			{
				// Стандартное сравнение суммы броска + модификаторов с Классом Сложности (DC)
				isSuccess = (total >= dc);
			}

			// 4. Формируем структуру итогового результата
			FinalRoll result = new FinalRoll
			{
				DiceValue = selectedValue,
				Modifier = modifier,
				Total = total,
				TargetDC = dc,
				IsSuccess = isSuccess,
				IsCriticalSuccess = isNatMax,
				IsCriticalFailure = isNat1,
				Timestamp = DateTime.Now
			};

			return result;
		}
	}
}
```

---

### Алгоритм 4: Физика Броска и Ограничение Зоны (`DiceController.cs`)

**Описание:** Модуль управляет приложением физических импульсов/вращающих моментов (`AddForce`, `AddTorque`), удерживает кубик в границах круговой арены через векторов отражения (Reflection vector) и отслеживает состояние полной остановки (`IsSleeping`).

```csharp
using UnityEngine;

namespace BG3DiceSystem.Gameplay.Dice
{
	[RequireComponent(typeof(Rigidbody))]
	public class DiceController : MonoBehaviour
	{
		public Rigidbody RigidBody;
		public Vector3 RollCenter = new Vector3(1000f, 1000f, 0f);
		public float RollZoneRadius = 1.2f;
		private float _sleepThreshold = 0.05f;

		/// <summary>
		/// Прикладывает случайный физический импульс и вращение к кубику.
		/// </summary>
		public void ThrowDice()
		{
			if (RigidBody == null) return;

			RigidBody.isKinematic = false;

			// Генерируем случайное направление в плоскости броска
			Vector2 randomCircle = Random.insideUnitCircle.normalized;
			Vector3 forceDir = new Vector3(randomCircle.x, randomCircle.y, 0f);
			float forceMag = Random.Range(6.0f, 9.5f);

			// Прикладываем линейную силу броска
			RigidBody.AddForce(forceDir * forceMag, ForceMode.Impulse);

			// Прикладываем случайный 3D вращающий момент для эффектного кувыркания
			Vector3 torque = Random.onUnitSphere * Random.Range(30f, 50f);
			RigidBody.AddTorque(torque, ForceMode.Impulse);
		}

		private void FixedUpdate()
		{
			if (RigidBody == null || RigidBody.isKinematic) return;

			// Фиксируем координату Z для 2.5D плоскости броска
			Vector3 pos = transform.position;
			pos.z = RollCenter.z;

			// Проверяем вылет за радиус зоны броска
			Vector2 offset = new Vector2(pos.x - RollCenter.x, pos.y - RollCenter.y);
			float dist = offset.magnitude;

			if (dist > RollZoneRadius)
			{
				// Отражаем вектор скорости от виртуальной круговой стены
				Vector2 dir = offset.normalized;
				pos.x = RollCenter.x + dir.x * RollZoneRadius;
				pos.y = RollCenter.y + dir.y * RollZoneRadius;

				Vector3 vel = RigidBody.linearVelocity;
				Vector2 vel2D = new Vector2(vel.x, vel.y);

				if (Vector2.Dot(vel2D, dir) > 0)
				{
					vel2D = Vector2.Reflect(vel2D, -dir) * 0.7f; // Упругий отскок со сжатием
					RigidBody.linearVelocity = new Vector3(vel2D.x, vel2D.y, 0f);
				}
			}
			else
			{
				// Мягкое притяжение к центру для красивого закручивания в центре экрана
				Vector2 springForce = -offset * 3.0f;
				RigidBody.AddForce(new Vector3(springForce.x, springForce.y, 0f), ForceMode.Acceleration);
			}

			transform.position = pos;
		}

		/// <summary>
		/// Определяет, остановился ли кубик окончательно.
		/// </summary>
		public bool IsSleeping()
		{
			if (RigidBody == null) return true;
			return RigidBody.isKinematic || 
				  (RigidBody.linearVelocity.sqrMagnitude < _sleepThreshold * _sleepThreshold && 
				   RigidBody.angularVelocity.sqrMagnitude < _sleepThreshold * _sleepThreshold);
		}
	}
}
```

---

### Алгоритм 5: Последовательная Анимация UI Модификаторов (`ResultView.cs`)

**Описание:** Корутина поочередно спавнит карточки бонусов/модификаторов (например, "+3 Ловкость", "+2 Наставление"), плавно суммирует их значения с числом кубика, а затем с задержкой открывает статусный бейдж "УСПЕХ / НЕУДАЧА" и запускает звуки и VFX.

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using BG3DiceSystem.Gameplay.Roll;

namespace BG3DiceSystem.UI
{
	public class ResultView : MonoBehaviour
	{
		public TextMeshProUGUI TotalText;
		public TextMeshProUGUI StatusBadgeText;
		public float DisplayDurationSeconds = 3.5f;

		/// <summary>
		/// Запускает пошаговую анимацию применения модификаторов и отображения итога.
		/// </summary>
		public void ShowResult(FinalRoll roll, List<ModifierCardData> modifiers)
		{
			gameObject.SetActive(true);
			StartCoroutine(AnimateResultSequence(roll, modifiers));
		}

		private IEnumerator AnimateResultSequence(FinalRoll roll, List<ModifierCardData> modifiers)
		{
			int currentRunningTotal = roll.DiceValue;
			TotalText.text = currentRunningTotal.ToString();

			// 1. Поочередное прибавление каждого активного модификатора
			if (modifiers != null)
			{
				foreach (var mod in modifiers)
				{
					yield return new WaitForSeconds(0.45f); // Пауза перед каждым бонусом

					// Спавн и анимация прилета карточки модификатора...
					currentRunningTotal += mod.Value;
					TotalText.text = currentRunningTotal.ToString(); // Обновляем счетчик
				}
			}

			// 2. Пауза перед кульминацией открывания результата
			yield return new WaitForSeconds(0.6f);

			// 3. Отображаем бейдж результата (Успех / Неудача)
			StatusBadgeText.gameObject.SetActive(true);
			StatusBadgeText.text = roll.IsSuccess ? "УСПЕХ" : "НЕУДАЧА";

			// 4. Воспроизводим итоговые аудио эффекты победы/поражения
			if (roll.IsSuccess)
			{
				_audioService?.PlayVictoryChime();
				_effectsService?.PlaySuccessParticles();
			}
			else
			{
				_audioService?.PlayFailureSound();
			}

			// 5. Ожидаем установленное время показа перед возвратом в исходное состояние
			yield return new WaitForSeconds(DisplayDurationSeconds);
			gameObject.SetActive(false);
		}
	}
}
```

---

## 3. Сводка Архитектурных Решений

1. **Dependency Injection (Zenject):** Все сервисы (`DiceService`, `RollService`, `AudioService`, `SkillService`) внедряются через интерфейсы, обеспечивая слабую связность (Loose Coupling) и простоту модульного тестирования.
2. **Event-Driven Architecture:** UI-компоненты подписываются на события сервисов, устраняя циклические зависимости и регулярный опрос (polling).
3. **Легковесная Анимация:** Реализован собственная система твининга (`Tweening`), исключающая сторонние зависимости для плавной работы на любых платформах.
