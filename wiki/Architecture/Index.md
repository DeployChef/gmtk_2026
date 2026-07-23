# Architecture — Index

Техническая архитектура **They will descend**. Опирается на [[../GDD/00 Overview|GDD]].

> **Правило:** эти заметки — план и контракт. Сцены / префабы / `.meta` — человек в Unity; пошаговые инструкции по Editor — в чате, не в вики. Код — агент после согласования.

## Документы

- [[00 Overview]] — обзор стека и слоёв
- [[01 Folder Structure]] — `Assets/_Project`
- [[02 Scenes & Root LifetimeScope]] — вечная Root-сцена и родительский scope
- [[03 Audio Manager]] — аудио и BPM
- [[04 Game Director]] — оркестратор сессии
- [[05 Event Bus]] — шина событий

## Связь с GDD

| GDD | Архитектура |
| --- | --- |
| [[../GDD/01 Gameplay Loop\|Gameplay Loop]] | [[04 Game Director]], геймплей-системы |
| [[../GDD/02 Entities\|Entities]] | NPC, карточки |
| [[../GDD/03 Economy\|Economy]] | здания, слоты, крафт |
| [[../GDD/04 Timeline & Events\|Timeline]] | таймлайн + [[05 Event Bus]] |
| [[../GDD/06 UI & Visual\|UI]] | HUD, сайдбар, камера |
| [[../GDD/07 Narrative & Onboarding\|Onboarding]] | диалоги Шамана |
