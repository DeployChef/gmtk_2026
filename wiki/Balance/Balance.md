# Balance — метод и кривая сложности

← [[../Home|Home]] · GDD: [[../GDD/03 Economy|Economy]] · [[../GDD/04 Timeline & Events|Timeline]]

Рабочая таблица: [`TheyWillDescend_Balance.xlsx`](TheyWillDescend_Balance.xlsx)  
Пересборка: `python wiki/Balance/generate_balance_xlsx.py`

---

## Зачем так

Баланс — **борьба со временем и жёсткой нехваткой** (ресурсы + люди). Сразу крутить все 8 фаз бессмысленно: слишком много степеней свободы.

**Метод:** этап за этапом через Cheat Panel (Jump to phase) → зафиксировать исход → это стартовые данные следующей фазы. Когда линейка стабильна — склеивать **пары** фаз, чтобы таймер ходил «параболой» (непонятно, выигрываешь или нет).

---

## Кривая feel

| Phase | Title (сейчас) | Feel | Diff | Slack (wall-time) | Intent |
| --- | --- | --- | --- | --- | --- |
| 0 | Dawn Offering | легко | 1 | 15..25s | Water+Wood; научить DnD |
| 1 | Drought Harvest | средне | 2 | 10..18s | Farm build; corn+water; −25% Water |
| 2 | Harvest Pressure | сложнее | 3 | 5..12s | Давление corn; рычаг −% Farm |
| 3 | Obsidian Idols | сложно | 4 | 3..8s | Obsidian на 3 workers — пик дефицита людей |
| 4 | Mixed Tribute | **на пределе** | 5 | 0..5s | Параллельный оффер — всё не покрыть |
| 5 | First Blood | **легко / развитие** | 2 | 15..30s | Передышка: освоить Blood/Altar, накопить |
| 6 | Twin Demands | сложно | 4 | 3..8s | Twin; подготовить StartTimer для финала |
| 7 | Final Propitiation | **очень сложно** | 5 | 0..5s | Финал; после последнего дара doomsday **5..15с** |

Фаза **5 специально легче фазы 4** — контраст перед дугой 6→7. Если 5 ощущается так же жёстко, как 4 — кривая сломана.

---

## Building IDs (сцена Game, актуальные)

Порядок ID = порядок прогрессии. Снято со сцены `Game.unity` (override `buildingId` + recipe).

| Id | GO name | Recipe | Output |
| --- | --- | --- | --- |
| **0** | HumanFarm | Home (`Recipe_Human`) | Villager |
| **1** | Well | Well (`Recipe_Well`) | Water |
| **2** | Lumber | Lumber (`Recipe_Lumber`) | Wood |
| **3** | Farm | Farm (`Recipe_Farm`) | Corn |
| **4** | StoneCave | StoneCave (`Recipe_Stone`) | Stone |
| **5** | ObsidianCave | Obsidian (`Recipe_Obsidian`) | Obsidian |
| **6** | GoldMine | GoldCave (`Recipe_Gold`) | Gold |
| **7** | Altar | Altar (`Recipe_Altar`) | Blood |

### Что сейчас в timeline (проверить при балансе фазы 1)

| Данные | Сейчас | Заметка |
| --- | --- | --- |
| `runStartBuildings` | 0, 1, 2 Built | Home, Well, Lumber |
| Phase 0 `unlockBuildingIds` | 0, 1, 2, **3** | Home, Well, Lumber, **Farm** |
| Phase 1 `unlockBuildingIds` | **4** (StoneCave) | камень |

StoneCave = **4**, имеет смысл открывать на фазе 1 (Stone & Timber). Obsidian = **5** — позже.

CheatPanel loadouts всё ещё Built 0/1/2 — ок для старта; для Jump на mid-game нужно будет обновить.

### Playtest log — Phase 0 / 1

- Phase 0: **3 Water + 3 Wood** @ +2s; duration **50s**. Wood **5с**, Water **3с**. 2nd villager: **3 Wood** + Home craft **15с**. Farm unlock с старта (build 5W+3Water).
- Phase 1: duration **90с**, Corn **+12** / Water **+6**. Cheat Jump: timer **61с** + 2p / 3 water / 4 wood.


_(сюда твои мысли после прогона)_

---

## Рычаги сложности

Трогать в таком порядке:

1. **Оффер** — count / duration / `secondsReward`
2. **Люди** — сколько дают, hire-стоимость (план), перестановки между джобами
3. **Production modifiers** по фазе (−% / +% скорости) — **в коде** + чипы на TopBar
4. **Early unlock** зданий «не для текущего оффера» — стратегия стока наперёд

### Production modifiers (в коде)

На `PhaseDefinition.productionModifiers[]`. Тултип + иконка на TopBar (`EraModifierHudView`).

Формула: `EffectiveProdSec = BaseProdSec / (1 + SpeedPct/100)`.  
Тик: `_progress += dt * (1 + SpeedPct/100)` (несколько матчей — перемножаются).

Черновик по фазам (в `GameTimelineConfig`):

| Phase | Draft modifier |
| --- | --- |
| 0 | нет |
| 1 | −10% Stone |
| 2 | **−20% Corn** |
| 3 | −15% Obsidian |
| 4 | −10% All |
| 5 | **+5% All** + **+10% Blood** (передышка) |
| 6 | −20% Obsidian, −10% Blood |
| 7 | −15% All |

Подробные title/description правятся в Inspector на каждом модификаторе.

---

## Два часа

| Часы | UI | Проигрыш? |
| --- | --- | --- |
| Таймлайн фаз | TopBar | Нет (провал оффера → гнев → дальше) |
| Doomsday timer | над пирамидой | Да, `00:00` |

Баланс **не** обязан возвращать таймер к 99. Допустимы провалы и пики; важен feel фазы и финальная посадка.

---

## Финальная посадка (фаза 7)

После **последней** сданной карты оффера на doomsday должно остаться **5–15 секунд**.

```
Landing = StartTimer + OfferGain − ElapsedWhenLastCard
```

Цель: `5 ≤ Landing ≤ 15`.

**Проблема текущих цифр:** maxGain фазы 7 = `5×22 + 4×15 + 10×3 = 200`. При Elapsed ≈ 110–120 окно недостижимо при разумном StartTimer.

Практичный коридор:

- MaxGain фазы 7 ≈ **40..80**
- StartTimer с фазы 6 ≈ **50..90**
- Elapsed закрытия ≈ **100..120**

Крутить: резать `secondsReward` на 7, душить таймер на 5–6, усложнять крафт (modifiers / люди).

---

## Люди как дефицит

Сейчас Home пассивно штампует Villager. План: каждый следующий житель — **оффер** с растущей ценой (как пирамида) → контролируем темп.

Цель: людей **меньше**, чем желаемых рабочих слотов → таскать между джобами или ставить двоих на одну ветку (ускорение ценой другой).

> Скорость крафта: **1 / 1.5 / 2** (+0.5× за каждого сверх `workersRequired`).

Черновик hire-офферов и target headcount — лист `07_Villagers`.

---

## Процесс плейтеста одной фазы

1. Jump to phase N (loadout ≈ Start из `05_Carryover`).
2. Сыграть только эту фазу.
3. Записать в Carryover: EndTimer, карты, люди, здания, stress 1–5, «felt vs target».
4. End N → Start N+1 (в таблице — формулами).
5. Синхронизировать `CheatPanelConfig.phaseLoadouts` под реалистичный Start.

Потом пары: особенно **4→5** (сброс после предела) и **6→7** (зажим + landing).

---

## Листы Excel

| Лист | Назначение |
| --- | --- |
| `00_Method` | Метод и рычаги |
| `01_Buildings` | Рецепты / gaps |
| `02_Phases` | Офферы + feel |
| `03_TimeCost` | Цена времени единицы ресурса |
| `03b_Modifiers` | −%/+% по фазам + матрица рычагов |
| `04_Phase_Lab` | Лаба одной фазы (жёлтое = INPUT) |
| `05_Carryover` | End → Start между фазами |
| `06_Final_Landing` | Посадка 5..15с |
| `07_Villagers` | Дефицит / hire-офферы |
| `08_Pair_Curve` | Кривая таймера на паре |
| `09_Checklist` | Порядок работ |

Цвета: **жёлтый** = крутить руками, **зелёный** = формула.

---

## Gaps (закрыть до серьёзного баланса)

1. **Нет / проверить unlock Stone (id 4)** на фазе 1 — сейчас в timeline unlock **5 (Obsidian)**.
2. **Altar (id 7)** — когда unlock? Логично фаза 5 (передышка/Blood).
3. ~~**Workers не ускоряют**~~ — сделано (**1 / 1.5 / 2**).
4. **Hire-оффер жителей** — вместо пассивного Home (если ещё так).

---

## Источники цифр

- `Assets/_Project/Data/Timeline/GameTimelineConfig.asset`
- `Assets/_Project/Data/Buildings/Recipe_*.asset`
- `Assets/_Project/Data/Cheats/CheatPanelConfig.asset`

Связанные: [[../GDD/03 Economy|Economy]] · [[../GDD/04 Timeline & Events|Timeline]] · [[../Architecture/07 Timeline & Pyramid|Arch: Timeline]] · [[../Architecture/08 Buildings|Arch: Buildings]]
