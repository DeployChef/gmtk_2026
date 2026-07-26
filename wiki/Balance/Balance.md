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
| 3 | Obsidian Idols | **на пределе** | 5 | 0..5s | Obsidian+Stone+Wood оффер; −15% Obsidian |
| 4 | Breathing Room | **легко / передышка** | 2 | 15..30s | Unlock Gold; +10% Water/Wood; много Water+Corn |
| 5 | First Blood | средне / обучение | 2 | 10..20s | Unlock Altar; 1 Blood |
| 6 | Final Propitiation | **очень сложно** | 5 | 0..5s | 5 Gold + 3 Obs + 6 Blood; landing **~20с** |

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
| Phase 3 `unlockBuildingIds` | **5** | Obsidian |
| Phase 4 `unlockBuildingIds` | **6** | Gold only (Altar → P5) |
| Phase 5 `unlockBuildingIds` | **7** | Altar |

StoneCave = **4** на фазе 1. Obsidian = **5** на фазе 3. Gold = **6** на фазе 4. Altar = **7** на фазе 5.

CheatPanel loadouts всё ещё Built 0/1/2 — ок для старта; для Jump на mid-game нужно будет обновить.

### Playtest log — Phase 0 / 1

- Phase 0: cards Water/Wood **+1**; offer-complete **+16**; duration **60с**. Total gain **22с**.
- Phase 1: Corn **+11** / Water **+5**; complete **+18**; duration **100с**.
- Phase 2: Corn **+7**; complete **+20**; duration **100с**.
- Phase 3: cards **3 Obs @ +5** / **4 Stone @ +5** / **8 Wood @ +12**; complete **+65**; duration **150с**. Cheat Jump P4: timer **75с**.
- Phase 4 **Breathing Room**: start **75с**, duration **170с**; offer **12 Water @ +7** + **8 Corn @ +7**; complete **+45** (= **185с** gain) → ideal end **90с** (`75 − 170 + 185`). **+10% Water / +10% Wood**. Gold build **8 Corn + 6 Stone**, craft **30с**. 6th hire = **3 Gold**.
- Phase 5 **First Blood**: start **90с**, duration **100с**; …; complete **+30**; **−10% Wood**. Altar **3 Stone + 3 Wood + 3 Gold + 2 Obs**; Blood **7.333с**. 7th hire = **6 Gold**.
- Phase 6 **Final**: start **70с**, duration **110с**; offer **5 Gold @ +2** + **3 Obs @ +4** + **6 Blood @ +2**; complete **+26** (= **60с** gain) → ideal end **20с** (`70 − 110 + 60`). **−15% All except Blood** + **−10% Gold**. Blood **7.333с**.


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
| 4 | **+10% Water** + **+10% Wood** (передышка) |
| 5 | нет / **−10% Wood** |
| 6 | **−15% All except Blood** + **−10% Gold** (финал) |

Подробные title/description правятся в Inspector на каждом модификаторе.

---

## Два часа

| Часы | UI | Проигрыш? |
| --- | --- | --- |
| Таймлайн фаз | TopBar | Нет (провал оффера → гнев → дальше) |
| Doomsday timer | над пирамидой | Да, `00:00` |

Баланс **не** обязан возвращать таймер к 99. Допустимы провалы и пики; важен feel фазы и финальная посадка.

**Fail mercy:** `GameTimelineConfig.failMercyFloorSeconds` (**60**). При провале оффера (или expire не на финале) таймер поднимается до пола, если ниже. Expire на не-финале = фейл фазы + mercy, не мгновенный луз. Expire на финале = луз.

---

## Offer complete bonus

На `PhaseDefinition.offerCompleteBonusSeconds`: один раз при сдаче **последней** карты оффера.

Ideal end = `Start − Duration + Σ(cardRewards) + completeBonus`.


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

> Скорость крафта: **1 / 1.75 / 2.5** (+0.75× за каждого сверх `workersRequired`).

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

1. ~~**unlock Stone / Obsidian / Gold / Altar**~~ — P1=4, P3=5, P4=6+7.
2. Loadouts фаз **4–6** после плейтеста P3.
3. ~~**Workers не ускоряют**~~ — сделано (**1 / 1.75 / 2.5**).
4. Hire: 2nd 3 Wood → 3rd 4 Corn → 4th 3 Stone → 5th 2 Obs+2 Corn → 6th 3 Gold → **7th 6 Gold** (clamp).

---

## Источники цифр

- `Assets/_Project/Data/Timeline/GameTimelineConfig.asset`
- `Assets/_Project/Data/Buildings/Recipe_*.asset`
- `Assets/_Project/Data/Cheats/CheatPanelConfig.asset`

Связанные: [[../GDD/03 Economy|Economy]] · [[../GDD/04 Timeline & Events|Timeline]] · [[../Architecture/07 Timeline & Pyramid|Arch: Timeline]] · [[../Architecture/08 Buildings|Arch: Buildings]]
