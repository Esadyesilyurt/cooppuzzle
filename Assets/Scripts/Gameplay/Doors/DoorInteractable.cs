using CoopPuzzle.Gameplay.Questions;
using CoopPuzzle.Questions;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Doors
{
  [RequireComponent(typeof(DoorQuestionSlot))]
  public sealed class DoorInteractable : MonoBehaviour
  {
    [SerializeField] private DoorQuestionSlot questionSlot;
    [SerializeField] private Collider blockingCollider;
    [SerializeField] private float interactDistance = 2.5f;

    [Header("Görsel (opsiyonel)")]
    [SerializeField] private Transform doorLeaf;
    [SerializeField] private float openYawDegrees = 90f;

    private bool _isOpen;
    private Quaternion _closedRotation;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
      if (questionSlot == null)
        questionSlot = GetComponent<DoorQuestionSlot>();

      if (blockingCollider == null)
        blockingCollider = GetComponent<Collider>();

      if (doorLeaf != null)
        _closedRotation = doorLeaf.localRotation;
    }

    public void TryInteractFromTraveler(Vector3 travelerPosition)
    {
      if (_isOpen) return;

      var dist = Vector3.Distance(travelerPosition, transform.position);
      if (dist > interactDistance)
      {
        Debug.Log("Kapıya daha yaklaş.");
        return;
      }

      var flow = FindAnyObjectByType<QuestionFlowController>();
      if (flow == null)
      {
        Debug.LogError("QuestionFlowController sahnede yok.");
        return;
      }

      flow.RequestQuestion(this);
    }

    public QuestionData GetQuestion() => questionSlot != null ? questionSlot.AssignedQuestion : null;

    public void Open()
    {
      if (_isOpen) return;
      _isOpen = true;

      if (blockingCollider != null)
        blockingCollider.enabled = false;

      if (doorLeaf != null)
        doorLeaf.localRotation = _closedRotation * Quaternion.Euler(0f, openYawDegrees, 0f);
    }
  }
}
