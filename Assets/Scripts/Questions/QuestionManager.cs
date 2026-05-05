using System.Collections.Generic;
using UnityEngine;

namespace CoopPuzzle.Questions
{
    public sealed class QuestionManager : MonoBehaviour
    {
        [Header("Kaynak")]
        [SerializeField] private QuestionDatabase database;

        [Header("Hedef kapılar")]
        [Tooltip("Boş bırakırsan sahne yüklendiğinde tüm DoorQuestionSlot bulunur.")]
        [SerializeField] private List<DoorQuestionSlot> doorSlots = new();

        [Header("Dağıtım")]
        [SerializeField] private bool avoidRepeats = true;
        [SerializeField] private bool assignOnStart = true;

        private void Start()
        {
            if (assignOnStart)
                AssignQuestionsToDoors();
        }

        /// <summary>
        /// Veri tabanındaki soruları kapılara rastgele dağıtır.
        /// </summary>
        public void AssignQuestionsToDoors()
        {
            if (database == null)
            {
                Debug.LogError("QuestionManager: QuestionDatabase atanmadı.");
                return;
            }

            var slots = ResolveDoorSlots();
            if (slots.Count == 0)
            {
                Debug.LogWarning("QuestionManager: DoorQuestionSlot bulunamadı.");
                return;
            }

            var pool = new List<QuestionData>();
            foreach (var q in database.EnumerateValidQuestions())
                pool.Add(q);

            if (pool.Count == 0)
            {
                Debug.LogWarning("QuestionManager: Geçerli soru yok (QuestionData eksik/invalid).");
                return;
            }

            var rng = new System.Random();
            var used = new HashSet<QuestionData>();

            foreach (var slot in slots)
            {
                if (slot == null) continue;
                slot.Clear();

                QuestionData picked = PickQuestion(pool, used, rng);
                if (picked == null)
                    continue;

                slot.SetAssignedQuestion(picked);
            }
        }

        private QuestionData PickQuestion(IReadOnlyList<QuestionData> pool, HashSet<QuestionData> used, System.Random rng)
        {
            if (!avoidRepeats)
                return pool[rng.Next(0, pool.Count)];

            // Unique dağıtım: mümkün değilse tekrara düş.
            var candidates = new List<QuestionData>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                var q = pool[i];
                if (!used.Contains(q))
                    candidates.Add(q);
            }

            if (candidates.Count == 0)
                return pool[rng.Next(0, pool.Count)];

            var choice = candidates[rng.Next(0, candidates.Count)];
            used.Add(choice);
            return choice;
        }

        private List<DoorQuestionSlot> ResolveDoorSlots()
        {
            if (doorSlots != null && doorSlots.Count > 0)
            {
                var list = new List<DoorQuestionSlot>();
                foreach (var s in doorSlots)
                    if (s != null) list.Add(s);
                return list;
            }

            return new List<DoorQuestionSlot>(FindObjectsByType<DoorQuestionSlot>(FindObjectsInactive.Exclude));
        }
    }
}
