using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class DialogueMenuDisplay : MenuDisplayBase
{
    [Header("Components")]
    [SerializeField] private PlayerUIManager _playerUIManager;

    [Header("Text Fields")]
    [SerializeField] private TextMeshProUGUI _nameField;
    [SerializeField] private TextMeshProUGUI _dialogueField;

    [Header("UI Elements")]
    [SerializeField] private GameObject _nameFieldHolder;
    [SerializeField] private GameObject _dialogueCompleteIcon;

    [Header("Audio")]
    private AudioClip finishDialogueSFX;// The sound that plays when the dialogue has stopped typing
    private AudioSource audioSource;

    [Header("System")]
    private DialogueNode currentNode;
    private Coroutine displayingLinesCoroutine = null;
    private bool isCurrentDialogueSkipped;
    private EDialogueState currentDialogueState;

    [field: Header("System")]
    public event Action StartDialogueEvent;
    public event Action<DialogueNode> DialogueEvent;

    public enum EDialogueState
    {
        None,
        Typing,
        WaitingForInput,
        PlayerOptions,
    }

    #region Initialization Methods

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        _playerUIManager = GetComponentInChildren<PlayerUIManager>();
    }
    #endregion

    #region Input Methods

    public override void SetControlsSubscription(bool isInputEnabled)
    {
        if (isInputEnabled)
            Subscribe();
        else if (InputManager.Instance != null)
            Unsubscribe();
    }

    public override void Subscribe()
    {
        InputManager.Instance.controls.Permanents.Interact.performed += TryInput;
        InputManager.Instance.controls.Permanents.Pause.performed += CloseDialogueMenu;
    }

    public override void Unsubscribe()
    {
        InputManager.Instance.controls.Permanents.Interact.performed -= TryInput;
        InputManager.Instance.controls.Permanents.Pause.performed -= CloseDialogueMenu;
    }

    public void TryInput(InputAction.CallbackContext context)
    {
        if (currentDialogueState == EDialogueState.Typing)
        {
            isCurrentDialogueSkipped = true;
        }
        else if (currentDialogueState == EDialogueState.WaitingForInput)
        {
            // Cache prev note
            DialogueNode prevNode = currentNode;

            // Load next node
            if (currentNode && currentNode.FollowingNode != null)
                DisplayDialogueNode(currentNode.FollowingNode);
            else CloseDialogueMenu();

            // Invoke dialogue event based on prev node
            DialogueEvent?.Invoke(prevNode);
        }
    }
    #endregion

    #region Dialogue Methods

    public void DisplayDialogueNode(DialogueNode node)
    {
        // Check if the window is opened for the first time in a conversation
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);

            StartDialogueEvent?.Invoke();

            // Set this as the active UI display
            _playerUIManager.SetActiveDisplay(this);
        }

        // Check if the text has a associated name
        currentNode = node;
        _nameFieldHolder.SetActive(currentNode.DisplayName != "");

        // Clear text fields
        _nameField.text = "";
        _dialogueField.text = "";

        // Reset complete icon
        _dialogueCompleteIcon.SetActive(false);

        // Stop any dialogue currently playing
        if (displayingLinesCoroutine != null)
            StopAllCoroutines();

        // Display new dialogue
        displayingLinesCoroutine = StartCoroutine(PrintText(currentNode));
    }

    public void CloseDialogueMenu()
    {
        // Clear current dialogue
        currentNode = null;

        // Disable this display if it is the active one
        if (_playerUIManager.GetActiveDisplay() == this)
            _playerUIManager.DisableActiveDisplay();

        currentDialogueState = EDialogueState.None;

        //Cancel all ongoing methods
        StopAllCoroutines();

        // Clear text fields
        _nameField.text = "";
        _dialogueField.text = "";

        _dialogueCompleteIcon.SetActive(false);
    }

    public void CloseDialogueMenu(InputAction.CallbackContext context)
    {
        CloseDialogueMenu();
    }

    //Displays dialogue text
    private IEnumerator PrintText(DialogueNode node)
    {
        // Fill name field
        _nameField.text = node.DisplayName;

        //Get the dialogues text and begin typing
        string displayText = node.Text;

        isCurrentDialogueSkipped = false;
        currentDialogueState = EDialogueState.Typing;

        // Print dialogue
        foreach (char letter in displayText.ToCharArray())
        {
            if (isCurrentDialogueSkipped)
            {
                // Play finish sounds
                if (finishDialogueSFX)
                    audioSource.PlayOneShot(finishDialogueSFX);

                _dialogueCompleteIcon.SetActive(true);
                _dialogueField.text = node.Text;

                // Wait two seconds until completion
                yield return new WaitForSeconds(.2f);

                break;
            }
            else
            {
                _dialogueField.text += letter;

                // Play typing sound for anything that is not a space
                if (letter != ' ' && node.TypeClip)
                    audioSource.PlayOneShot(node.TypeClip);

                yield return new WaitForSeconds(node.PrintSpeed);
            }
        }

        // Complete
        _dialogueCompleteIcon.SetActive(true);
        _dialogueField.text = node.Text;
        currentDialogueState = EDialogueState.WaitingForInput;
    }
    #endregion

}
