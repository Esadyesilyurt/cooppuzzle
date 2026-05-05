using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoopPuzzle.Questions
{
    [CreateAssetMenu(menuName = "CoopPuzzle/Questions/Question Data", fileName = "QuestionData")]
    public sealed class QuestionData : ScriptableObject
    {
        [TextArea(2, 6)]
        [SerializeField] private string questionText;

        [SerializeField] private string[] options = Array.Empty<string>();

        [Min(0)]
        [SerializeField] private int correctOptionIndex;

        [SerializeField] private string category;

        [Range(1, 5)]
        [SerializeField] private int difficulty = 1;

        public string QuestionText => questionText;
        public IReadOnlyList<string> Options => options;
        public int CorrectOptionIndex => correctOptionIndex;
        public string Category => category;
        public int Difficulty => difficulty;

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(questionText))
            {
                error = "QuestionText boş.";
                return false;
            }

            if (options == null || options.Length < 2)
            {
                error = "En az 2 seçenek olmalı.";
                return false;
            }

            if (correctOptionIndex < 0 || correctOptionIndex >= options.Length)
            {
                error = "CorrectOptionIndex seçenek aralığının dışında.";
                return false;
            }

            error = null;
            return true;
        }
    }
}

