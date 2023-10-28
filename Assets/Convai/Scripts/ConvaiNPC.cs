#if UNITY_ANDROID
using UnityEngine.Android;
#endif

using System.Collections;
using System.Collections.Generic;
using Convai.Scripts.Utils;
using Grpc.Core;
using Service;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Convai.Scripts.Utils.ConvaiLipSync;

// This script uses gRPC for streaming and is a work in progress
// Edit this script directly to customize your intelligent NPC character

namespace Convai.Scripts
{
    [RequireComponent(typeof(Animator), typeof(AudioSource))]
    [AddComponentMenu("Convai/ConvaiNPC")]
    [HelpURL(
        "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/overview-of-the-convainpc.cs-script")]
    public class ConvaiNPC : MonoBehaviour
    {
        private const int AUDIO_SAMPLE_RATE = 44100;
        private const string GRPC_API_ENDPOINT = "stream.convai.com";

        private const int RECORDING_FREQUENCY = AUDIO_SAMPLE_RATE;
        private const int RECORDING_LENGTH = 30;
        private static readonly int Talk = Animator.StringToHash("Talk");

        public string sessionID = "-1";

        [HideInInspector] public string stringCharacterText = "";

        [SerializeField] public string characterID;

        [SerializeField] public string characterName;

        [HideInInspector] public Animator characterAnimator;

        [SerializeField] public bool isCharacterActive;

        [Space(10)] [Header("Include Components")] [Tooltip("Include Actions Handler component")]
        public bool includeActionsHandler;

        [Tooltip("Include LipSync component")] public bool includeLipSync;

        [Tooltip("Include HeadEyeTracking component")]
        public bool includeHeadEyeTracking;

        [Tooltip("Include Blinking component")]
        public bool includeBlinking;

        private readonly List<ResponseAudio> _responseAudios = new();

        private ActionConfig _actionConfig;

        private ConvaiActionsHandler _actionsHandler;

        private bool _animationPlaying;

        private AudioSource _audioSource;

        private Channel _channel;
        private ConvaiChatUIHandler _chatUIHandler;
        private ConvaiService.ConvaiServiceClient _client;

        private ConvaiCrosshairHandler _convaiCrosshairHandler;

        private ConvaiGlobalActionSettings _globalActionSettings;

        private ConvaiGRPCAPI _grpcAPI;

        private bool _isActionActive;
        private bool _isLipSyncActive;
        private ConvaiLipSync _lipSyncHandler;

        private bool _playingStopLoop;

        // do not edit
        [HideInInspector] public List<GetResponseResponse> GetResponseResponses = new();

        private void Awake()
        {
            // Find and assign references to various components and handlers using FindObjectOfType

            // Find and assign the ConvaiGRPCAPI component in the scene
            _grpcAPI = FindObjectOfType<ConvaiGRPCAPI>();

            // Find and assign the ConvaiChatUIHandler component in the scene
            _chatUIHandler = FindObjectOfType<ConvaiChatUIHandler>();

            // Find and assign the ConvaiGlobalActionSettings component in the scene
            _globalActionSettings = FindObjectOfType<ConvaiGlobalActionSettings>();

            // Find and assign the ConvaiCrosshairHandler component in the scene
            _convaiCrosshairHandler = FindObjectOfType<ConvaiCrosshairHandler>();

            // Get the AudioSource component attached to the same GameObject
            _audioSource = GetComponent<AudioSource>();

            // Get the Animator component attached to the same GameObject
            characterAnimator = GetComponent<Animator>();

            // Check if a ConvaiActionsHandler component is attached to this GameObject
            if (GetComponent<ConvaiActionsHandler>())
            {
                // If present, set the action handling flag to true
                _isActionActive = true;

                // Get the ConvaiActionsHandler component and its action configuration
                _actionsHandler = GetComponent<ConvaiActionsHandler>();
                _actionConfig = _actionsHandler.ActionConfig;
            }

            // Check if a ConvaiLipSync component is attached to this GameObject
            if (GetComponent<ConvaiLipSync>())
            {
                // If present, set the lip-sync handling flag to true
                _isLipSyncActive = true;

                // If present, get the ConvaiLipSync component
                _lipSyncHandler = GetComponent<ConvaiLipSync>();
            }
        }

        private void Start()
        {
            // Start the coroutine that plays audio clips in order
            StartCoroutine(PlayAudioInOrder());

            // Check if the platform is Android
#if UNITY_ANDROID
        // Check if the user has not authorized microphone permission
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            // Request microphone permission from the user
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif

            // do not edit
            // gRPC setup configuration

            #region GRPC_SETUP

            // Create SSL credentials for secure communication
            SslCredentials credentials = new();


            // Initialize a gRPC channel with the specified endpoint and credentials
            _channel = new Channel(GRPC_API_ENDPOINT, credentials);

            // Initialize the gRPC client for the ConvaiService using the channel
            _client = new ConvaiService.ConvaiServiceClient(_channel);

            #endregion
        }

        private void Update()
        {
            // This block handles starting and stopping audio recording and processing
            if (isCharacterActive)
            {
                // Start recording audio when the Space key is pressed
                if (Input.GetKeyDown(KeyCode.T))
                {
                    // Check if action configuration is null to determine if actions functionality should be active
                    bool isActionActive = _actionConfig == null;

                    // If action configuration exists and ConvaiCrosshairHandler is available,
                    // update the current attention object in the action configuration
                    if (_actionConfig != null && _convaiCrosshairHandler != null)
                        _actionConfig.CurrentAttentionObject = _convaiCrosshairHandler.FindPlayerReferenceObject();

                    // Start recording audio using the StartListening method
                    StartListening();
                }

                // Stop recording audio when the Space key is released
                if (Input.GetKeyUp(KeyCode.T))
                    // Stop recording audio using the StopListening method
                    StopListening();
            }

            // Reload the scene when 'R' key and '=' key are pressed simultaneously
            if (Input.GetKey(KeyCode.R) && Input.GetKey(KeyCode.Equals))
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);

            // Quit the application when 'Escape' key and '=' key are pressed simultaneously
            if (Input.GetKey(KeyCode.Escape) && Input.GetKey(KeyCode.Equals)) Application.Quit();

            // If there are response objects in the queue, process and play the next one
            if (GetResponseResponses.Count > 0)
            {
                // Process the response at the front of the queue
                ProcessResponse(GetResponseResponses[0]);

                // Remove the processed response from the queue
                GetResponseResponses.Remove(GetResponseResponses[0]);
            }

            // If there are audio clips being played, trigger the talking animation
            if (_responseAudios.Count > 0)
            {
                if (_animationPlaying == false)
                {
                    // Enable the talking animation based on response
                    // Typically, set to talk animation when audio is playing
                    _animationPlaying = true;
                    characterAnimator.SetBool(Talk, true);
                }
            }
            else
            {
                if (_animationPlaying)
                {
                    // Deactivate animations and transition to idle animation
                    _animationPlaying = false;
                    characterAnimator.SetBool(Talk, false);
                }
            }
        }

        /// <summary>
        ///     Unity callback that is invoked when the application is quitting.
        ///     Stops the loop that plays audio in order.
        /// </summary>
        private void OnApplicationQuit()
        {
            // Set the flag to stop the loop that plays audio
            _playingStopLoop = true;
        }

        /// <summary>
        ///     Initiates the audio recording process using the gRPC API.
        /// </summary>
        public void StartListening()
        {
            // Start recording audio using the ConvaiGRPCAPI's StartRecordAudio method
            // Pass necessary parameters such as client, action activity status, recording details, and character information
            _grpcAPI.StartRecordAudio(_client, _isActionActive, _isLipSyncActive, RECORDING_FREQUENCY, RECORDING_LENGTH,
                characterID, _actionConfig);
        }

        /// <summary>
        ///     Stops the ongoing audio recording process.
        /// </summary>
        public void StopListening()
        {
            // Stop the audio recording process using the ConvaiGRPCAPI's StopRecordAudio method
            _grpcAPI.StopRecordAudio();
        }

        /// <summary>
        ///     Processes a response fetched from a character.
        /// </summary>
        /// <param name="getResponseResponse">
        ///     The getResponseResponse object that will be processed to add the audio and transcript
        ///     to the playlist
        /// </param>
        /// <remarks>
        ///     1. Processes audio/text/face data from the response and adds it to _responseAudios.
        ///     2. Identifies actions from the response and parses them for execution.
        /// </remarks>
        private void ProcessResponse(GetResponseResponse getResponseResponse)
        {
            // Check if the character is active and should process the response
            if (isCharacterActive)
            {
                if (getResponseResponse.AudioResponse != null)
                {
                    // Initialize empty strings for text and face data
                    string textDataString = "";
                    string faceDataString = "";

                    // Check if text data exists in the response
                    if (getResponseResponse.AudioResponse.TextData != null)
                        textDataString = getResponseResponse.AudioResponse.TextData;

                    // Check if face data exists in the response
                    if (getResponseResponse.AudioResponse.FaceData != null)
                        faceDataString = getResponseResponse.AudioResponse.FaceData;

                    // Convert audio data from proto format to byte array
                    byte[] byteAudio = getResponseResponse.AudioResponse.AudioData.ToByteArray();

                    // Process byte audio data to create an AudioClip
                    AudioClip clip = _grpcAPI.ProcessByteAudioDataToAudioClip(byteAudio,
                        getResponseResponse.AudioResponse.AudioConfig.SampleRateHertz.ToString());

                    // Add the response audio along with associated data to the list
                    _responseAudios.Add(new ResponseAudio
                    {
                        AudioClip = clip,
                        AudioTranscript = textDataString,
                        FaceData = faceDataString
                    });
                }
                // Check if the response contains action data
                else if (getResponseResponse.ActionResponse is { Action: not null })
                    // Check if the action field is not null
                    // If an actions handler is available, parse the action
                {
                    if (_actionsHandler != null)
                        _actionsHandler.ParseActions(getResponseResponse.ActionResponse.Action);
                }
            }
        }

        /// <summary>
        ///     Plays audio clips attached to characters in the order of responses.
        /// </summary>
        /// <returns>
        ///     A IEnumerator that can facilitate coroutine functionality
        /// </returns>
        /// <remarks>
        ///     1. Starts a loop that plays audio from response, and performs corresponding actions and animations.
        ///     2. Loop continues until the application quits.
        /// </remarks>
        private IEnumerator PlayAudioInOrder()
        {
            // Continuously play audio as long as the stop loop flag is false
            while (!_playingStopLoop)
                // Check if there are audio clips in the playlist
                if (_responseAudios.Count > 0)
                {
                    // Set the current audio clip to play
                    _audioSource.clip = _responseAudios[0].AudioClip;
                    _audioSource.Play();

                    // If chat UI handler is available, indicate that the character is talking
                    if (_chatUIHandler != null)
                    {
                        _chatUIHandler.isCharacterTalking = true;

                        // Display the audio transcript in the chat UI
                        _chatUIHandler.SendCharacterText(characterName, _responseAudios[0].AudioTranscript);
                    }

                    // If lip sync handler is available and lip sync is enabled,
                    // perform lip syncing based on the response audio
                    if (_lipSyncHandler != null)
                        if (_lipSyncHandler.convaiLipSyncType != LipSyncType.None)
                            _lipSyncHandler.LipSyncCharacter(_responseAudios[0]);
                    // Wait for the audio clip's length before proceeding
                    yield return new WaitForSeconds(_responseAudios[0].AudioClip.length);

                    // Stop the audio playback and reset audio source
                    _audioSource.Stop();
                    _audioSource.clip = null;

                    // If chat UI handler is available, indicate that the character stopped talking
                    if (_chatUIHandler != null) _chatUIHandler.isCharacterTalking = false;

                    // Remove the played audio clip from the playlist
                    _responseAudios.Remove(_responseAudios[0]);

                    // If lip sync handler is available and lip sync is enabled,
                    // reset the character's lip syncing
                    if (_lipSyncHandler != null)
                        if (_lipSyncHandler.convaiLipSyncType != LipSyncType.None)
                            _lipSyncHandler.ResetCharacterLips();
                }
                else
                {
                    // If no audio clips are in the playlist, yield to the next frame
                    yield return null;
                }
        }

        public class ResponseAudio
        {
            public AudioClip AudioClip;
            public string AudioTranscript;
            public string FaceData;
        }
    }
}