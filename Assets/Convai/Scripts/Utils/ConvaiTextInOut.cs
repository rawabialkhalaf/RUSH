using System;
using System.Threading.Tasks;
using Grpc.Core;
using Service;
using TMPro;
using UnityEngine;
using static Service.GetResponseRequest.Types;

namespace Convai.Scripts.Utils
{
    public class ConvaiTextInOut : MonoBehaviour
    {
        private const string GRPC_API_ENDPOINT = "stream.convai.com";
        [HideInInspector] public string APIKey;

        [Tooltip("Character ID ")] public string characterID;

        [Tooltip("Session ID")] public string sessionID;

        [Header("UI References")] public TMP_Text responseText;

        public TMP_InputField userInput;

        private AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call;
        private ConvaiService.ConvaiServiceClient client;
        private string responseString;

        /// <summary>
        ///     Load API Key from ConvaiAPIKeySetup scriptable object
        /// </summary>
        private void Awake()
        {
            ConvaiAPIKeySetup APIKeyScriptableObject = Resources.Load<ConvaiAPIKeySetup>("ConvaiAPIKey");

            if (APIKeyScriptableObject != null)
                APIKey = APIKeyScriptableObject.APIKey;
            else
                Debug.LogError(
                    "No API Key data found. Please complete the Convai Setup. In the Menu Bar, click Convai > Setup.");
        }

        /// <summary>
        ///     Set up gRPC client and channel
        /// </summary>
        private void Start()
        {
            SslCredentials credentials = new();

            // The IP Address could be down
            Channel channel = new(GRPC_API_ENDPOINT, credentials);

            client = new ConvaiService.ConvaiServiceClient(channel);
        }

        /// <summary>
        ///     Listen for Return key press to send text data to Convai servers
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SendTextData();
                responseString = "";
            }

            responseText.text = responseString;
        }


        private async Task SendTextData()
        {
            #region Setting up gRPC and intializing the stream

            // Setting up gRPC and initializing the stream
            call = client.GetResponse();

            // Creating the configuration request for GetResponseConfig
            GetResponseRequest getResponseConfigRequest = new()
            {
                GetResponseConfig = new GetResponseConfig
                {
                    CharacterId = characterID,
                    ApiKey = APIKey,
                    SessionId = sessionID,
                    AudioConfig = new AudioConfig
                    {
                        DisableAudio = true
                    }
                }
            };

            try
            {
                // Sending the configuration request to the server
                await call.RequestStream.WriteAsync(getResponseConfigRequest);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }

            #endregion

            #region Starting the response function

            // Starting the response function in a separate Task
            Task task = Task.Run(async () => { await ReceiveResultFromServer(call); });

            #endregion

            #region Sending the text data to Convai servers

            // Sending the user's text input to Convai servers
            string userText = userInput.text;

            try
            {
                await call.RequestStream.WriteAsync(new GetResponseRequest
                {
                    GetResponseData = new GetResponseData
                    {
                        TextData = userText
                    }
                });
            }
            catch (RpcException rpcException)
            {
                if (rpcException.StatusCode == StatusCode.Cancelled)
                    Debug.LogException(rpcException);
                else
                    throw;
            }

            #endregion

            #region Closing the stream after all the data is sent

            // Closing the stream after all the data is sent
            await call.RequestStream.CompleteAsync();

            #endregion
        }

        private async Task ReceiveResultFromServer(
            AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call)
        {
            // Continuously iterate through the response stream from the server
            while (await call.ResponseStream.MoveNext())
                try
                {
                    // Get the current response from the stream
                    GetResponseResponse result = call.ResponseStream.Current;

                    // Check if the response has an AudioResponse
                    if (result.AudioResponse != null)
                        // Append the text data from the AudioResponse to the response string
                        responseString += result.AudioResponse.TextData;

                    // Update the sessionID with the current response's session ID
                    sessionID = call.ResponseStream.Current.SessionId;
                }
                catch (RpcException rpcException)
                {
                    if (rpcException.StatusCode == StatusCode.Cancelled)
                        // Log any cancellation exceptions
                        Debug.LogException(rpcException);
                    else
                        // Throw the exception for other cases
                        throw;
                }
        }
    }
}