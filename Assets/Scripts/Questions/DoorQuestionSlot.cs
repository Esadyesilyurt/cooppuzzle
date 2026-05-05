using UnityEngine;

namespace CoopPuzzle.Questions
{
    /// <summary>
    /// Labirentteki bir kapıya atanmış soruyu tutar. UI’yi değiştirmez; sadece veri taşır.
    /// </summary>
    public sealed class DoorQuestionSlot : MonoBehaviour
    {
        [SerializeField] private QuestionData assignedQuestion;

        public QuestionData AssignedQuestion => assignedQuestion;

        public void SetAssignedQuestion(QuestionData question) => assignedQuestion = question;

        public void Clear() => assignedQuestion = null;
    }
}
