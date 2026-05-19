# Coop Puzzle

An online, team-based knowledge race. Two teams (Traveler + Sage) explore a shared map, solve door challenges, and race to the finish zone—the first team to arrive wins.

**Unity** · **Netcode for GameObjects** · **Unity Gaming Services (Lobby + Relay)**

---

## Game Introduction

**Coop Puzzle** is a cooperative online puzzle–race set on a shared map. Two teams (each with a **Traveler** and a **Sage**) explore the world, solve **door challenges**, and race to the **finish zone**. The Traveler moves and answers questions; the Sage uses a **master document** to guide the team. The first team to reach the goal wins. Success depends on **communication, role division, and quick use of knowledge** under time pressure.

---

## What I Did (Individual Contributions)

*   **Esad Yeşilyurt:** 
    *   Developed the core game loop and networked logic using Netcode for GameObjects.
    *   Scripted the Traveler movement, interaction mechanics, and door challenge triggers.
    *   Integrated Unity Gaming Services (Lobby & Relay) for seamless multiplayer matchmaking.
*   **Bünyamin Aslan:** 
    *   Designed and implemented the Start Menu, Lobby interfaces, and the final Win/Loss screens.
    *   Created the in-game UI, including the Sage's master document interface and the interactive question pop-ups for the Traveler.
*   **Muhammed Raşit Algan:** 
    *   Created 3D models for the environment, including the shared map layout, interactive doors, and the finish zone.
    *   Designed and integrated the visual assets for the Traveler and Sage character representations.
*   **Kayra Cem Gökmen:** 
    *   Designed and integrated sound effects and audio feedback (e.g., correct/wrong answer sounds, UI clicks).
    *   Authored the educational questions and configured the `QuestionData` system to populate the door challenges in the editor.

---

## Educational Concept

### Pedagogical goal

The game teaches **collaborative knowledge retrieval and application**: learners recall information (from the Sage document), communicate it clearly, and use it to solve door questions and reach the goal. Learning content is defined by the **questions attached to doors** (configured in `QuestionData` / door question slots in the editor—for example curriculum facts, concepts, or procedures for your subject).

### Learning theory

**Constructivism (primary), with Cognitivist elements**

- **Constructivism:** Knowledge is built through **social interaction** (Sage ↔ Traveler). The Sage mediates understanding via the document and verbal coordination; the Traveler applies it in context at each door.
- **Cognitivism:** Doors act as **retrieval practice**; immediate feedback (e.g. a movement penalty after a wrong answer) reinforces correct associations and reduces guessing.

### How game mechanics facilitate learning

| Mechanic | Learning function |
|----------|-------------------|
| **Traveler + Sage roles** | Splits **information access** and **action**, encouraging explanation, listening, and shared problem-solving instead of isolated guessing. |
| **Master document (Sage)** | Supports **guided discovery** and **reference skills** (finding the right section when a door opens). |
| **Door questions** | **Retrieval and application** at the moment of need (“just-in-time” learning). |
| **Wrong-answer penalty** | Immediate **feedback**; encourages careful reasoning before answering. |
| **Team race to finish** | Adds **motivation** and **strategic cooperation** (when to move, when to stop and discuss). |

**Example:** If a door asks for a historical date, the Traveler cannot answer reliably alone; the Sage must **locate** it in the document and **communicate** it. That mirrors authentic learning: **use sources and teamwork**, not only rote memorization.

---

## Roles & teams

| Slot | Team | Role |
|------|------|------|
| 0 | Red (Team 1) | Traveler |
| 1 | Red (Team 1) | Sage |
| 2 | Blue (Team 2) | Traveler |
| 3 | Blue (Team 2) | Sage |

---

## Scenes

| Scene | Purpose |
|-------|---------|
| `Assets/Scenes/menü.unity` | Lobby / main menu |
| `Assets/Scenes/SampleScene.unity` | Gameplay map |

---

## Quick setup (Unity Editor)

1. Open the project in **Unity 6000.x** (see `ProjectSettings/ProjectVersion.txt`).
2. Run **Tools → CoopPuzzle → Setup → Phase 1 Setup (One Click)** (bootstrap).
3. Run **Phase 5 / 6** setup for lobby → gameplay and networked players.
4. Map: **Tools → CoopPuzzle → Map** (import map, spawns, doors, finish zone).
5. Test with two editor instances or **ParrelSync** (host + client).

Link Unity Gaming Services via **Tools → CoopPuzzle → Setup → Check Unity Cloud Link (UGS)**.

---

## License

See the repository owner for license terms.
