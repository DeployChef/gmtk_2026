# Balance — метод и кривая сложности

← [[../Home|Home]] · GDD: [[../GDD/03 Economy|Economy]] · [[../GDD/04 Timeline & Events|Timeline]]

Рабочая таблица: [`TheyWillDescend_Balance.xlsx`](TheyWillDescend_Balance.xlsx)  
Пересборка: `python wiki/Balance/generate_balance_xlsx.py`

---

## Зачем так

Баланс — **борьба со временем и жёсткой нехваткой** (ресурсы + люди). Сразу крутить все **7** фаз бессмысленно: слишком много степеней свободы.

**Метод:** этап за этапом через Cheat Panel (Jump to phase) → зафиксировать исход → это стартовые данные следующей фазы. Когда линейка стабильна — склеивать **пары** фаз, чтобы таймер ходил «параболой» (непонятно, выигрываешь или нет).

---

## Кривая feel

| Phase | Title | Feel | Diff | Slack (wall-time) | Intent |
| --- | --- | --- | --- | --- | --- |
| 0 | Dawn Offering | легко | 1 | 15..25s | Water+Wood; научить DnD |
| 1 | Drought Harvest | средне | 2 | 10..18s | Farm build; corn+water; −25% Water |
| 2 | Harvest Pressure | сложнее | 3 | 5..12s | Давление corn; рычаг −% Farm |
| 3 | Obsidian Idols | **на пределе** | 5 | 0..5s | Obsidian на 3 workers — пик дефицита людей |
| 4 | Breathing Room | **легко / передышка** | 2 | 15..30s | Unlock Altar; освоить Blood |
| 5 | Blood and Grain | средне | 3 | 8..15s | Blood + Corn |
| 6 | Final Propitiation | **очень сложно** | 5 | 0..5s | Gold + Obsidian + Blood; людей в кровь; landing **5..15с** |

**7 фаз (0..6).** Пик середины = **3**. Передышка = **4**. Финал = **6** (золото + идолы + кровь; все рабочие → алтарь если надо).

Убраны: Mixed Tribute, Twin Demands (слиты в новую дугу 4→5→6).

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
| Phase 3 `unlockBuildingIds` | **5, 6** | Obsidian + Gold |
| Phase 4 `unlockBuildingIds` | **7** (Altar) | Blood |

StoneCave = **4** на фазе 1. Obsidian + Gold = **5, 6** на фазе 3. Altar = **7** на фазе 4.

CheatPanel loadouts всё ещё Built 0/1/2 — ок для старта; для Jump на mid-game нужно будет обновить.

### Playtest log — Phase 0 / 1

- Phase 0: **3 Water + 3 Wood** @ +2s; duration **50s**. Wood **5с**, Water **3с**. 2nd villager: **3 Wood** + Home craft **15с**. 3rd: **4 Corn**. Farm unlock с старта (build 5W+3Water).
- Phase 1: duration **90с**, Corn **+12** / Water **+6**. Cheat Jump: timer **61с** + 2p / 3 water / 4 wood; Built Home/Well/Lumber.
- Phase 2: offer **10 Corn** @ **+8s**. Ideal end ≈ **32с** (`42 − 90 + 80`). Cheat Jump: timer **42с** + 2p / 3 water / 2 wood; Built Home/Well/Lumber/Farm/Stone. Home hire (2p → оффер **4 Corn**).
- Phase 3: Cheat Jump: timer **38с** + 3p / 1 water / 2 wood / 1 corn; Built Home/Well/Lumber/Farm/Stone; unlock **Obsidian (5) + Gold (6)**. Offer now: **4 Obsidian** @ **+18s**, duration **100с**, −15% Obsidian. Ideal end ≈ `38 − 100 + 72 = **10с**` (если оффер полный).
- Phase 4 **Breathing Room**: unlock Altar; **2 Blood** @ +20; +5% All / +10% Blood. (loadout TBD после плейтеста P3)
- Phase 5 **Blood and Grain**: **3 Blood** @ +22 + **8 Corn** @ +6. (loadout TBD)
- Phase 6 **Final**: **3 Gold** @ +18 + **3 Obsidian** @ +18 + **4 Blood** @ +20; −15% All; landing **5..15с**; людей → алтарь.


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
| 1 | **−25% Water** |
| 2 | **−20% Corn** |
| 3 | **−15% Obsidian** (пик) |
| 4 | **+5% All** + **+10% Blood** (передышка) |
| 5 | нет |
| 6 | **−15% All** (финал) |

Подробные title/description правятся в Inspector на каждом модификаторе.

---

## Два часа

| Часы | UI | Проигрыш? |
| --- | --- | --- |
| Таймлайн фаз | TopBar | Нет (провал оффера → гнев → дальше) |
| Doomsday timer | над пирамидой | Да, `00:00` |

Баланс **не** обязан возвращать таймер к 99. Допустимы провалы и пики; важен feel фазы и финальная посадка.

---

## Финальная посадка (фаза 6)

После **последней** сданной карты оффера на doomsday должно остаться **5–15 секунд**.

```
Landing = StartTimer + OfferGain − ElapsedWhenLastCard
```

Цель: `5 ≤ Landing ≤ 15`.

Оффер финала: **Gold + Obsidian + Blood**. Intent: нехватка людей → жертвовать рабочих на алтарь.

Крутить: `secondsReward` / count на 6, StartTimer с фазы 5, craft / modifiers.

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

1. ~~**unlock Stone / Obsidian / Gold / Altar**~~ — P1=4, P3=5+6, P4=7.
2. Loadouts фаз **4–6** после плейтеста P3.
3. ~~**Workers не ускоряют**~~ — сделано (**1 / 1.5 / 2**).
4. Hire-офферы 4-го+ жителя (сейчас clamp на 4 Corn).

---

## Источники цифр

- `Assets/_Project/Data/Timeline/GameTimelineConfig.asset`
- `Assets/_Project/Data/Buildings/Recipe_*.asset`
- `Assets/_Project/Data/Cheats/CheatPanelConfig.asset`

Связанные: [[../GDD/03 Economy|Economy]] · [[../GDD/04 Timeline & Events|Timeline]] · [[../Architecture/07 Timeline & Pyramid|Arch: Timeline]] · [[../Architecture/08 Buildings|Arch: Buildings]]
