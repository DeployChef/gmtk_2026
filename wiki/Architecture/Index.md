# Architecture — Index

Техническая архитектура **They will descend**. Опирается на [[../GDD/00 Overview|GDD]].

> **Правило:** эти заметки — план и контракт. Сцены / префабы / `.meta` — человек в Unity; пошаговые инструкции по Editor — в чате, не в вики. Код — агент после согласования.

## Документы

- [[00 Overview]] — обзор стека и слоёв
- [[01 Folder Structure]] — `Assets/_Project`
- [[02 Scenes & Root LifetimeScope]] — вечная Root-сцена и родительский scope
- [[03 Audio Manager]] — аудио
- [[04 Game Director]] — оркестратор сессии
- [[05 Event Bus]] — шина событий
- [[06 Inventory]] — лотки, стеки, ResourceDefinition, burn
- [[07 Timeline & Pyramid]] — фазы, офферы, таймер Конца Света, гнев богов
- [[08 Buildings]] — слоты, стройка-оффер, production, молния только Built

## Связь с GDD

| GDD | Архитектура |
| --- | --- |
| [[../GDD/01 Gameplay Loop\|Gameplay Loop]] | [[04 Game Director]], геймплей-системы |
| [[../GDD/02 Entities\|Entities]] | NPC, карточки · [[06 Inventory]] |
| [[../GDD/03 Economy\|Economy]] | [[08 Buildings]] · слоты, стройка, крафт, ResourceDefinition |
| [[../GDD/04 Timeline & Events\|Timeline]] | [[07 Timeline & Pyramid]] + [[05 Event Bus]] |
| [[../GDD/05 Win Lose\|Win/Lose]] | таймер пирамиды · [[07 Timeline & Pyramid]] |
| [[../GDD/06 UI & Visual\|UI]] | HUD, лотки, камера, сегменты таймлайна |
| [[../GDD/07 Narrative & Onboarding\|Onboarding]] | диалоги Шамана |
