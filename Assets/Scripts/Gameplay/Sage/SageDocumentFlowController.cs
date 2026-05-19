using CoopPuzzle.Gameplay.Core;
using CoopPuzzle.Gameplay.Doors;
using CoopPuzzle.Gameplay.Map;
using CoopPuzzle.Gameplay.Questions;
using CoopPuzzle.Questions;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Sage
{
  public sealed class SageDocumentFlowController : MonoBehaviour
  {
    [SerializeField] private GameplaySessionConfig session;
    [SerializeField] private SageDocumentUI documentUI;
    [SerializeField] private SageMasterDocument masterDocument;
    [SerializeField] private SpawnTeam watchTeam = SpawnTeam.Team1;

    [Header("Görünürlük")]
    [Tooltip("Bilge Play'e girince ana belge açılsın.")]
    [SerializeField] private bool showMasterDocumentOnSageStart = true;

    [Tooltip("Gezgin kapıda soru çözünce belge öne gelsin (içerik aynı kalır).")]
    [SerializeField] private bool focusDocumentWhenTravelerAtDoor = true;

    [Tooltip("Soru bitince belgeyi kapatma — Bilge okumaya devam edebilir.")]
    [SerializeField] private bool closeDocumentWhenQuestionEnds;

    [TextArea(1, 3)]
    [SerializeField] private string doorActiveHint =
      "Gezgin bir kapıda soru çözüyor — bu belgede ilgili bilgiyi bul ve söyle.";

    private void Awake()
    {
      if (session == null)
        session = GameplaySessionConfig.Instance;

      if (documentUI == null)
        documentUI = FindAnyObjectByType<SageDocumentUI>();

    }

    private void OnEnable()
    {
      DoorGameplayEvents.TravelerDoorQuestionStarted += OnTravelerQuestionStarted;
      DoorGameplayEvents.TravelerDoorQuestionEnded += OnTravelerQuestionEnded;
    }

    private void OnDisable()
    {
      DoorGameplayEvents.TravelerDoorQuestionStarted -= OnTravelerQuestionStarted;
      DoorGameplayEvents.TravelerDoorQuestionEnded -= OnTravelerQuestionEnded;
    }

    private void Start()
    {
      if (session == null || session.LocalRole != GameplayRole.Sage)
        return;

      if (showMasterDocumentOnSageStart)
        ShowMasterDocument(hint: null);

      if (IsTravelerQuestionActive())
        ShowMasterDocument(doorActiveHint);
    }

    private void OnTravelerQuestionStarted(DoorInteractable door, QuestionData data, SpawnTeam travelerTeam)
    {
      if (!ShouldReactToTeam(travelerTeam))
        return;

      if (focusDocumentWhenTravelerAtDoor)
        ShowMasterDocument(doorActiveHint);
    }

    private void OnTravelerQuestionEnded(DoorInteractable door, SpawnTeam travelerTeam)
    {
      if (session == null || session.LocalRole != GameplayRole.Sage)
        return;

      if (!ShouldReactToTeam(travelerTeam))
        return;

      if (closeDocumentWhenQuestionEnds)
        documentUI?.Hide();
    }

    public void RefreshActiveDocument()
    {
      if (session == null || session.LocalRole != GameplayRole.Sage)
        return;

      if (IsTravelerQuestionActive())
        ShowMasterDocument(doorActiveHint);
      else if (showMasterDocumentOnSageStart)
        ShowMasterDocument(hint: null);
      else
        documentUI?.Hide();
    }

    public void ShowMasterDocument(string hint = null)
    {
      if (session == null || session.LocalRole != GameplayRole.Sage)
        return;

      if (masterDocument == null)
      {
        Debug.LogWarning("SageMasterDocument atanmamış.");
        documentUI?.Show("Bilge Belgesi", "Master Document asset'i bağlanmadı.");
        return;
      }

      documentUI?.Show(masterDocument.Title, masterDocument.GetBody(), hint);
    }

    private bool ShouldReactToTeam(SpawnTeam travelerTeam)
    {
      if (session == null || session.LocalRole != GameplayRole.Sage)
        return false;

      return travelerTeam == watchTeam;
    }

    private bool IsTravelerQuestionActive()
    {
      var questionFlow = FindAnyObjectByType<QuestionFlowController>();
      return questionFlow != null && questionFlow.ActiveDoor != null;
    }

    public void SetWatchTeam(SpawnTeam team) => watchTeam = team;

    public void SetMasterDocument(SageMasterDocument document) => masterDocument = document;
  }
}
