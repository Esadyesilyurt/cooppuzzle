using System.Collections.Generic;
using UnityEngine;

namespace CoopPuzzle.Questions
{
    [CreateAssetMenu(menuName = "CoopPuzzle/Questions/Question Database", fileName = "QuestionDatabase")]
    public sealed class QuestionDatabase : ScriptableObject
    {
        [SerializeField] private List<QuestionData> questions = new();

        public IReadOnlyList<QuestionData> Questions => questions;

        public IEnumerable<QuestionData> EnumerateValidQuestions()
        {
            foreach (var q in questions)
            {
                if (q == null) continue;
                if (!q.IsValid(out _)) continue;
                yield return q;
            }
        }
    }
}

