using System.Collections;
using SpaceSeries.Core.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceSeries.Core.Dialogue
{
    /// <summary>
    /// Master coordinator for dialogue flow and state management.
    /// Handles typewriter skip, line progression, input actions, auto-advance, and post-dialogue events.
    /// </summary>
    public class DialogueRunner : MonoBehaviour, IDialogueRunner
    {
        [Header("Event Channel")]
        [Tooltip("The dialogue channel used to trigger and broadcast dialogue events.")]
        [SerializeField] private DialogueChannelSO _dialogueChannel;

        [Header("UI Reference")]
        [Tooltip("The dialogue UI view component.")]
        [SerializeField] private DialogueUI _dialogueUI;

        [Header("Input Handling")]
        [Tooltip("Optional Input Action for continuing/skipping dialogue (e.g. 'Player/Interact' or 'UI/Submit').")]
        [SerializeField] private InputActionReference _advanceAction;

        [Tooltip("Optional Input Action for skipping the entire dialogue sequence.")]
        [SerializeField] private InputActionReference _skipAllAction;

        [Header("Player Integration (Optional)")]
        [Tooltip("If assigned, astronaut locomotion is automatically paused while in dialogue.")]
        [SerializeField] private AstronautController _astronautController;

        private DialogueSequenceSO _currentSequence;
        private int _currentLineIndex = -1;
        private bool _isDialogueActive;
        private bool _isLineTyping;
        private Coroutine _autoAdvanceCoroutine;
        private bool _restoreLocomotion;
        public bool IsConfigured => _dialogueUI != null;

        // IDialogueRunner Properties
public bool IsDialogueActive => _isDialogueActive;
        public bool IsTyping => _isLineTyping;
        public DialogueSequenceSO CurrentSequence => _currentSequence;
        public DialogueLine CurrentLine => _currentSequence != null ? _currentSequence.GetLine(_currentLineIndex) : null;
        public int CurrentLineIndex => _currentLineIndex;

        private void Awake()
        {
            if (_dialogueUI == null)
            {
                _dialogueUI = FindFirstObjectByType<DialogueUI>();
            }

            if (_astronautController == null)
            {
                _astronautController = FindFirstObjectByType<AstronautController>();
            }
        }

        private void OnEnable()
        {
            if (_dialogueChannel != null)
            {
                _dialogueChannel.OnDialogueRequested += StartDialogue;
            }

            BindInputs();
        }

        private void OnDisable()
        {
            if (_dialogueChannel != null)
            {
                _dialogueChannel.OnDialogueRequested -= StartDialogue;
            }

            UnbindInputs();
            CancelDialogue();
        }

        public void CancelDialogue()
        {
            if (_autoAdvanceCoroutine != null) StopCoroutine(_autoAdvanceCoroutine);
            _autoAdvanceCoroutine = null;
            _isDialogueActive = false;
            _isLineTyping = false;
            _currentSequence = null;
            _currentLineIndex = -1;
            if (_dialogueUI != null) _dialogueUI.Hide();
            if (_restoreLocomotion && _astronautController != null && _astronautController.isActiveAndEnabled)
                _astronautController.SetLocomotionActive(true);
            _restoreLocomotion = false;
        }

        private void BindInputs()
{
            if (_advanceAction != null && _advanceAction.action != null)
            {
                _advanceAction.action.performed += HandleAdvanceInput;
                if (!_advanceAction.action.enabled)
                    _advanceAction.action.Enable();
            }

            if (_skipAllAction != null && _skipAllAction.action != null)
            {
                _skipAllAction.action.performed += HandleSkipAllInput;
                if (!_skipAllAction.action.enabled)
                    _skipAllAction.action.Enable();
            }
        }

        private void UnbindInputs()
        {
            if (_advanceAction != null && _advanceAction.action != null)
            {
                _advanceAction.action.performed -= HandleAdvanceInput;
            }

            if (_skipAllAction != null && _skipAllAction.action != null)
            {
                _skipAllAction.action.performed -= HandleSkipAllInput;
            }
        }

        private void HandleAdvanceInput(InputAction.CallbackContext context)
        {
            if (_isDialogueActive)
            {
                Advance();
            }
        }

        private void HandleSkipAllInput(InputAction.CallbackContext context)
        {
            if (_isDialogueActive)
            {
                SkipEntireDialogue();
            }
        }

        public void StartDialogue(DialogueSequenceSO sequence)
        {
            if (!isActiveAndEnabled || !IsConfigured || _isDialogueActive || sequence == null || sequence.LineCount == 0)
            {
                Debug.LogWarning("[DialogueRunner] Cannot start dialogue: runner/UI unavailable, busy, or sequence empty.");
                return;
            }

            _restoreLocomotion = _astronautController != null && _astronautController.isActiveAndEnabled && _astronautController.IsLocomotionActive;
            _currentSequence = sequence;
_currentLineIndex = 0;
            _isDialogueActive = true;

            // Pause astronaut movement during dialogue
            if (_astronautController != null)
            {
                _astronautController.SetLocomotionActive(false);
            }

            // Execute start actions
            ExecuteActions(_currentSequence.OnStartActions);

            _dialogueChannel?.RaiseDialogueStarted(_currentSequence);

            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            if (_autoAdvanceCoroutine != null)
            {
                StopCoroutine(_autoAdvanceCoroutine);
                _autoAdvanceCoroutine = null;
            }

            DialogueLine line = CurrentLine;
            if (line == null)
            {
                CompleteDialogue();
                return;
            }

            _isLineTyping = true;
            _dialogueChannel?.RaiseLineStarted(line, _currentLineIndex, _currentSequence.LineCount);

            _dialogueUI.DisplayLine(line, () =>
            {
                _isLineTyping = false;
                _dialogueChannel?.RaiseLineTypingCompleted(line);

                if (line.IsAutoAdvance)
                {
                    _autoAdvanceCoroutine = StartCoroutine(AutoAdvanceRoutine(line.AutoAdvanceDelay));
                }
            });
        }

        private IEnumerator AutoAdvanceRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            _autoAdvanceCoroutine = null;
            Advance();
        }

        public void Advance()
        {
            if (!_isDialogueActive)
                return;

            // If text is still typing, skip typewriter to full text immediately
            if (_isLineTyping)
            {
                SkipTyping();
                return;
            }

            // If line is done, run line-completed actions
            DialogueLine finishedLine = CurrentLine;
            if (finishedLine != null)
            {
                ExecuteActions(finishedLine.OnLineCompletedActions);
            }

            // Move to next line
            _currentLineIndex++;

            if (_currentLineIndex < _currentSequence.LineCount)
            {
                ShowCurrentLine();
            }
            else
            {
                CompleteDialogue();
            }
        }

        public void SkipTyping()
        {
            if (_isLineTyping)
            {
                _dialogueUI.SkipTyping();
            }
        }

        public void SkipEntireDialogue()
        {
            if (!_isDialogueActive || _currentSequence == null || !_currentSequence.AllowSkipEntireSequence)
                return;

            CompleteDialogue();
        }

        private void CompleteDialogue()
        {
            if (!_isDialogueActive)
                return;

            if (_autoAdvanceCoroutine != null)
            {
                StopCoroutine(_autoAdvanceCoroutine);
                _autoAdvanceCoroutine = null;
            }

            _isDialogueActive = false;
            _isLineTyping = false;

            DialogueSequenceSO completedSequence = _currentSequence;
            _dialogueUI.Hide();

            // Execute end actions (advancing quests, triggering cutscenes, etc.)
            ExecuteActions(completedSequence.OnEndActions);

            _dialogueChannel?.RaiseDialogueCompleted(completedSequence);

            // Check for chained sequence
            if (completedSequence.NextSequence != null)
            {
                StartDialogue(completedSequence.NextSequence);
                return;
            }

            // Re-enable astronaut movement
            if (_restoreLocomotion && _astronautController != null && _astronautController.isActiveAndEnabled)
            {
                _astronautController.SetLocomotionActive(true);
            }
            _restoreLocomotion = false;

            _currentSequence = null;
_currentLineIndex = -1;
        }

        public void StopDialogue()
        {
            CompleteDialogue();
        }

        private void ExecuteActions(DialogueActionSO[] actions)
        {
            if (actions == null || actions.Length == 0)
                return;

            DialogueExecutionContext context = new DialogueExecutionContext(
                _currentSequence,
                _currentSequence != null ? _currentSequence.LineCount : 0,
                gameObject);

            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i] != null)
                {
                    actions[i].Execute(context);
                }
            }
        }
    }
}
