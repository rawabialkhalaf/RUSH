using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Convai.Scripts.Utils
{
    // Enum to configure the type of UI to use  
    public enum UIType
    {
        Subtitle,
        QuestionAnswer,
        ChatBox
    }

    // Helper class to group properties of a Message
    internal class Message
    {
        public TMP_Text NpcName;
        public string Text;
        public TMP_Text TextObject;
    }


    /// <summary></summary>
    [AddComponentMenu("Convai/Chat UI Controller")]
    [DisallowMultipleComponent]
    [HelpURL(
        "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/scripts-overview/convaichatuihandler.cs")]
    public class ConvaiChatUIHandler : MonoBehaviour
    {
        private const int MAX_MESSAGE = 25; // limit on number of messages


        // Name and default text of each character
        [Header("Character settings")] [Tooltip("Display name of the character")]
        public string characterName = "Character";

        [Multiline] [Tooltip("Default text of the character")]
        public string characterText;

        [ColorUsage(true)] [Tooltip("Color of the character's text. Alpha value will be ignored.")]
        public Color characterTextColor = Color.white;

        // Name and default text of the user
        [Header("User settings")] [Tooltip("Display name of the user")]
        public string userName = "User";

        [Multiline] [Tooltip("Default text of the user")]
        public string userText;

        [ColorUsage(true)] [Tooltip("Color of the user's text. Alpha value will be ignored.")]
        public Color userTextColor = Color.white;

        [Header("UI Components")] [SerializeField] [Tooltip("GameObject which is active when the user is talking")]
        public GameObject userTalkingMarker; // UI for showing user speak indication

        [SerializeField] [Tooltip("TextMeshProUGUI component for showing the user's text")]
        public TextMeshProUGUI userTextField; // Reference to user's text field

        [SerializeField] [Tooltip("TextMeshProUGUI component for showing the character's text")]
        public TextMeshProUGUI characterTextField; // Reference to character's text field

        [Header("UI Settings")] [Tooltip("Is the chat UI currently visible")]
        public bool chatUIActive;

        [Tooltip("Is the character currently talking")]
        public bool isCharacterTalking;

        [Tooltip("Is the user currently talking")]
        public bool isUserTalking;

        [Tooltip("Type of UI to use")] public UIType uIType;


        private readonly List<Message> _messageList = new(); // list to hold messages

        // Reference to the game objects in scene (filled in Awake based on UI type)
        private GameObject _chatPanel;

        // Fields to hold the name of anyone currently speaking
        private string _currentlySpeakingCharacter = string.Empty;
        private GameObject _textObject;


        // On Awake, find necessary Game Objects based on UI type
        private void Awake()
        {
            switch (uIType)
            {
                case UIType.ChatBox:
                    _chatPanel = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject;
                    _textObject = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject;
                    break;
                case UIType.Subtitle:
                case UIType.QuestionAnswer:
                    // No additional setup needed
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // On Start, set default values for user and character names if not already set
        private void Start()
        {
            if (string.IsNullOrEmpty(characterName)) characterName = "Character";
            if (string.IsNullOrEmpty(userName)) userName = "User";
        }

        // On Update, refresh UI based on current state
        private void Update()
        {
            userTalkingMarker.SetActive(isUserTalking);

            // Handle UI updates based on type
            switch (uIType)
            {
                case UIType.Subtitle:
                {
                    // If the character is talking, update the userTextField with characterText
                    if (isCharacterTalking)
                        userTextField.text = characterText != "" ? $"<b>{characterName}</b>: {characterText}" : "";
                    // If the user is talking, update the userTextField with userText
                    else
                        userTextField.text = userText != "" ? $"<b>{userName}</b>: {userText}" : "";

                    break;
                }

                case UIType.QuestionAnswer:
                    userTextField.text = userText;
                    characterTextField.text =
                        $"<color=#{ColorUtility.ToHtmlStringRGB(characterTextColor)}>{characterText}</color>";
                    break;

                case UIType.ChatBox:
                    //...
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Process text coming from the character based on the UI type.
        /// </summary>
        /// <param name="charName">Name of the character.</param>
        /// <param name="text">Text to be processed.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SendCharacterText(string charName, string text)
        {
            switch (uIType)
            {
                case UIType.Subtitle:
                case UIType.QuestionAnswer:
                    characterText = text;
                    break;

                case UIType.ChatBox:
                    SendCharacterChatBoxMessage(charName, text);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Process text coming from the user based on the UI type.
        /// </summary>
        /// <param name="text">Text to be processed.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SendUserText(string text)
        {
            switch (uIType)
            {
                case UIType.Subtitle:
                case UIType.QuestionAnswer:
                    userText = text;
                    break;

                case UIType.ChatBox:
                    SendUserChatBoxMessage(text);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Handle the display of character's message in chat box UI.
        /// </summary>
        /// <param name="currentCharacterName">Name of the current speaking character.</param>
        /// <param name="text">The text message from current speaker.</param>
        private void SendCharacterChatBoxMessage(string currentCharacterName, string text)
        {
            // If this is the same character and there are already messages, append it
            if (_currentlySpeakingCharacter == currentCharacterName && _messageList.Count > 0)
            {
                _messageList[^1].Text += $"\n{text}";
                _messageList[^1].TextObject.text += $" {text}";
            }
            else
            {
                if (_messageList.Count >= MAX_MESSAGE)
                {
                    Destroy(_messageList[0].TextObject.gameObject);
                    _messageList.RemoveAt(0);
                }

                Message newMessage = new() { Text = text, NpcName = _textObject.GetComponent<TMP_Text>() };
                GameObject newText = Instantiate(_textObject, _chatPanel.transform);

                newMessage.TextObject = newText.GetComponent<TMP_Text>();
                newMessage.TextObject.text =
                    $"<b><color=#{ColorUtility.ToHtmlStringRGB(characterTextColor)}>{characterName}</color></b>: {text}";

                _messageList.Add(newMessage);
            }

            _currentlySpeakingCharacter = currentCharacterName;
        }

        /// <summary>
        ///     Handle the display of user's message in chat box UI.
        /// </summary>
        /// <param name="text">The text message from user.</param>
        private void SendUserChatBoxMessage(string text)
        {
            // If the user is talking, make sure to activate the chat UI and mark the user as not talking
            if (isUserTalking)
            {
                chatUIActive = true;
                isUserTalking = false;
            }

            if (chatUIActive)
            {
                chatUIActive = false;
                _currentlySpeakingCharacter = string.Empty; // Reset the currently speaking character

                if (_messageList.Count > 0 && _messageList[^1].NpcName.text == userName)
                {
                    _messageList[^1].Text += $"\n{text}";
                }
                else
                {
                    if (_messageList.Count >= MAX_MESSAGE)
                    {
                        Destroy(_messageList[0].TextObject.gameObject);
                        _messageList.RemoveAt(0);
                    }

                    Message newMessage = new() { Text = $"{text}", NpcName = _textObject.GetComponent<TMP_Text>() };
                    GameObject newText = Instantiate(_textObject, _chatPanel.transform);

                    newMessage.TextObject = newText.GetComponent<TMP_Text>();
                    newMessage.TextObject.text =
                        $"<b><color=#{ColorUtility.ToHtmlStringRGB(userTextColor)}>{userName}</color></b>: {text}";

                    _messageList.Add(newMessage);
                }
            }
            else
            {
                if (_messageList.Count > 0)
                    _messageList[^1].TextObject.text =
                        $"<b><color=#{ColorUtility.ToHtmlStringRGB(userTextColor)}>{userName}</color></b>: {text}";
            }
        }
    }
}