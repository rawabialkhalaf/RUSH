using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Service;
using UnityEngine;
using static Service.GetResponseRequest.Types;

namespace Convai.Scripts.Utils
{
    /// <summary>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Convai/Convai GRPC API")]
    [HelpURL(
        "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/scripts-overview/convaigrpcapi.cs")]
    public class ConvaiGRPCAPI : MonoBehaviour
    {
        public static ConvaiGRPCAPI Instance;

        public GameObject activeCharacter;

        [HideInInspector] public ConvaiNPC activeConvaiNPC;

        [HideInInspector] public string APIKey;

        //private string stringUserText = "";

        private readonly List<string> stringUserText = new();

        private ConvaiChatUIHandler chatUIHandler;

        private void Awake()
        {
            // Singleton pattern: Ensure only one instance of this script is active.
            if (Instance == null)
                Instance = this;
            else
                // If another instance exists, destroy this instance.
                Destroy(gameObject);

            // Load API key from a ScriptableObject in Resources folder.
            ConvaiAPIKeySetup APIKeyScriptableObject = Resources.Load<ConvaiAPIKeySetup>("ConvaiAPIKey");

            if (APIKeyScriptableObject != null)
                // If the API key data is found, assign it to the APIKey field.
                APIKey = APIKeyScriptableObject.APIKey;
            else
                // Log an error if API key data is missing.
                Debug.LogError(
                    "No API Key data found. Please complete the Convai Setup. In the Menu Bar, click Convai > Setup.");

            // Find and store a reference to the ConvaiChatUIHandler component in the scene.
            chatUIHandler = FindObjectOfType<ConvaiChatUIHandler>();
        }

        private void Update()
        {
            // Check if there are pending user texts to display
            if (stringUserText.Count > 0)
            {
                // If chatUIHandler is available, send the first user text in the list
                if (chatUIHandler != null)
                    chatUIHandler.SendUserText(stringUserText[0]);

                // Remove the displayed user text from the list
                stringUserText.RemoveAt(0);
            }
        }

        /// <summary>
        ///     This function is called when a collider enters the trigger zone of the GameObject.
        ///     It sets the active character based on the character the player is facing.
        /// </summary>
        /// <param name="other">The collider of the object that entered the trigger zone</param>
        private void OnTriggerEnter(Collider other)
        {
            // Check if the colliding object has the tag "Character" and is a ConvaiNPC
            if (other.tag == "Character" && other.gameObject.GetComponent<ConvaiNPC>() != null)
            {
                // Deactivate the previously active character if there was one
                if (activeCharacter != null)
                {
                    activeConvaiNPC = activeCharacter.GetComponent<ConvaiNPC>();
                    activeConvaiNPC.isCharacterActive = false;
                }

                // Set the new active character
                activeCharacter = other.gameObject;
                activeConvaiNPC = activeCharacter.GetComponent<ConvaiNPC>();

                // Activate the new active character
                activeConvaiNPC.isCharacterActive = true;
            }
        }

        /// <summary>
        ///     Converts an audio clip into WAV byte data.
        /// </summary>
        /// <param name="requestAudioClip">The audio clip to be converted</param>
        /// <returns>Byte array containing WAV audio data</returns>
        public byte[] ProcessRequestAudiotoWav(AudioClip requestAudioClip)
        {
            // Get audio data from the audio clip
            float[] floatAudioData = new float[requestAudioClip.samples];
            requestAudioClip.GetData(floatAudioData, 0);

            // Convert float data to Int16 data
            short[] intData = new short[floatAudioData.Length];
            int rescaleFactor = 32767; // Conversion factor to convert float to Int16

            // Convert float array to Int16 array
            for (int i = 0; i < floatAudioData.Length; i++) intData[i] = (short)(floatAudioData[i] * rescaleFactor);

            // Convert Int16 data to byte array (WAV format)
            byte[] bytesData = new byte[floatAudioData.Length * 2]; // Int16 is 2 bytes
            for (int i = 0; i < floatAudioData.Length; i++)
            {
                byte[] byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            // Add WAV header to the byte array and return it
            byte[] wavByteData = AddByteToArray(bytesData, requestAudioClip.frequency.ToString());
            return wavByteData;
        }

        /// <summary>
        ///     Converts a byte array representing 16-bit audio samples to a float array.
        /// </summary>
        /// <param name="source">Byte array containing 16-bit audio data</param>
        /// <returns>Float array containing audio samples in the range [-1, 1]</returns>
        private float[] Convert16BitByteArrayToFloatAudioClipData(byte[] source)
        {
            int x = sizeof(short); // Size of a short in bytes
            int convertedSize = source.Length / x; // Number of short samples
            float[] data = new float[convertedSize]; // Float array to hold the converted data

            int byte_idx = 0; // Index for the byte array
            int data_idx = 0; // Index for the float array

            // Convert each pair of bytes to a short and then to a float
            while (byte_idx < source.Length)
            {
                byte first_byte = source[byte_idx];
                byte second_byte = source[byte_idx + 1];
                byte_idx += 2;

                // Combine the two bytes to form a short (little endian)
                short s = (short)((second_byte << 8) | first_byte);

                // Convert the short value to a float in the range [-1, 1]
                data[data_idx] = s / 32768.0F; // Dividing by 32768.0 to normalize the range
                data_idx++;
            }

            return data;
        }

        /// <summary>
        ///     Converts string-encoded audio data to an AudioClip.
        /// </summary>
        /// <param name="audioData">String containing base64-encoded audio data</param>
        /// <param name="stringSampleRate">String representing the sample rate of the audio</param>
        /// <returns>AudioClip containing the decoded audio data</returns>
        public AudioClip ProcessStringAudioDataToAudioClip(string audioData, string stringSampleRate)
        {
            // Convert the base64-encoded audio data to a byte array
            byte[] byteAudio = Convert.FromBase64String(audioData);

            // Trim the 44 bytes WAV header to get the actual audio data
            byte[] trimmedByteAudio = new byte[byteAudio.Length - 44];

            for (int i = 0, j = 44; i < byteAudio.Length - 44; i++, j++) trimmedByteAudio[i] = byteAudio[j];

            // Convert the trimmed byte audio data to a float array of audio samples
            float[] samples = Convert16BitByteArrayToFloatAudioClipData(trimmedByteAudio);

            int channels = 1; // Mono audio
            int sampleRate = int.Parse(stringSampleRate); // Convert the sample rate string to an integer

            // Create an AudioClip using the converted audio samples and other parameters
            AudioClip clip = AudioClip.Create("ClipName", samples.Length, channels, sampleRate, false);
            clip.SetData(samples, 0); // Set the audio data for the AudioClip
            return clip;
        }

        /// <summary>
        ///     Converts a byte array containing audio data into an AudioClip.
        /// </summary>
        /// <param name="byteAudio">Byte array containing the audio data</param>
        /// <param name="stringSampleRate">String containing the sample rate of the audio</param>
        /// <returns>AudioClip containing the decoded audio data</returns>
        public AudioClip ProcessByteAudioDataToAudioClip(byte[] byteAudio, string stringSampleRate)
        {
            // Trim the 44 bytes WAV header from the byte array to get the actual audio data
            byte[] trimmedByteAudio = new byte[byteAudio.Length - 44];

            for (int i = 0, j = 44; i < byteAudio.Length - 44; i++, j++) trimmedByteAudio[i] = byteAudio[j];

            // Convert the trimmed byte audio data to a float array of audio samples
            float[] samples = Convert16BitByteArrayToFloatAudioClipData(trimmedByteAudio);

            int channels = 1; // Mono audio
            int sampleRate = int.Parse(stringSampleRate); // Convert the sample rate string to an integer

            // Create an AudioClip using the converted audio samples and other parameters
            AudioClip clip = AudioClip.Create("ClipName", samples.Length, channels, sampleRate, false);
            clip.SetData(samples, 0); // Set the audio data for the AudioClip
            return clip;
        }


        /// <summary>
        ///     Starts recording audio and sends it to the server for processing.
        /// </summary>
        /// <param name="client">gRPC service Client object</param>
        /// <param name="isActionActive">Bool specifying whether we are expecting action responses</param>
        /// <param name="recordingFrequency">Frequency of the audio being sent</param>
        /// <param name="recordingLength">Length of the recording from the microphone</param>
        /// <param name="characterID">Character ID obtained from the playground</param>
        /// <param name="actionConfig">Object containing the action configuration</param>
        public async Task StartRecordAudio(ConvaiService.ConvaiServiceClient client, bool isActionActive,
            bool isLipsyncActive, int recordingFrequency, int recordingLength, string characterID,
            ActionConfig actionConfig)
        {
            // Create gRPC call options with custom headers
            Metadata headers = new()
            {
                { "source", "Unity" }
            };

            CallOptions options = new(headers);

            // Start the duplex streaming call to the server
            AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call = client.GetResponse(options);

            // Set up the initial configuration for the GetResponse request
            GetResponseRequest getResponseConfigRequest = null;

            if (isActionActive || activeConvaiNPC != null)
                // Configure request with actions if needed
                getResponseConfigRequest = new GetResponseRequest
                {
                    GetResponseConfig = new GetResponseConfig
                    {
                        CharacterId = characterID,
                        ApiKey = APIKey,
                        SessionId = activeConvaiNPC.sessionID,
                        AudioConfig = new AudioConfig
                        {
                            SampleRateHertz = recordingFrequency,
                            EnableFacialData = isLipsyncActive
                        },
                        ActionConfig = actionConfig
                    }
                };
            else
                // Configure request without actions
                getResponseConfigRequest = new GetResponseRequest
                {
                    GetResponseConfig = new GetResponseConfig
                    {
                        CharacterId = characterID,
                        ApiKey = APIKey,
                        SessionId = activeConvaiNPC.sessionID,
                        AudioConfig = new AudioConfig
                        {
                            SampleRateHertz = recordingFrequency,
                            EnableFacialData = isLipsyncActive
                        }
                    }
                };

            try
            {
                // Send the initial request configuration to the server
                await call.RequestStream.WriteAsync(getResponseConfigRequest);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }

            // Start microphone recording
            AudioClip audioClip = Microphone.Start(null, false, recordingLength, recordingFrequency);

            if (chatUIHandler != null)
                chatUIHandler.isUserTalking = true;

            // Run the server response processing in a separate task
            Task task = Task.Run(async () => { await ReceiveResultFromServer(call); });

            float[] audioData = null;
            int pos = 0;

            int microphonePosition = Microphone.GetPosition(null);

            int diff = microphonePosition - pos;

            // Continue processing and sending audio chunks while recording
            while (Microphone.IsRecording(null))
            {
                // Wait for a period of time
                await Task.Delay(200);

                microphonePosition = Microphone.GetPosition(null);
                diff = microphonePosition - pos;

                // Get audio data from AudioClip and send it to the server
                audioData = new float[recordingFrequency * recordingLength];
                audioClip.GetData(audioData, pos);

                await ProcessAudioChunk(call, diff, audioData);

                pos += diff;
            }

            // Process any remaining audio data and complete the streaming call
            await ProcessAudioChunk(call, diff, audioData);
            await call.RequestStream.CompleteAsync();

            if (chatUIHandler != null)
                chatUIHandler.isUserTalking = false;
        }

        /// <summary>
        ///     Stops recording and processing the audio.
        /// </summary>
        public void StopRecordAudio()
        {
            // End microphone recording
            Microphone.End(null);
        }

        /// <summary>
        ///     Processes each audio chunk and sends it to the server.
        /// </summary>
        /// <param name="call">gRPC Streaming call connecting to the getResponse function</param>
        /// <param name="diff">Length of the audio data from the current position to the position of the last sent chunk</param>
        /// <param name="audioData">Chunk of audio data that we want to be processed</param>
        private async Task ProcessAudioChunk(AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call,
            int diff, float[] audioData)
        {
            if (diff > 0)
            {
                // Convert audio data to byte array
                byte[] audioByteArray = new byte[diff * sizeof(short)];

                for (int i = 0; i < diff; i++)
                {
                    float sample = audioData[i];
                    short shortSample = (short)(sample * short.MaxValue);
                    byte[] shortBytes = BitConverter.GetBytes(shortSample);
                    audioByteArray[i * sizeof(short)] = shortBytes[0];
                    audioByteArray[i * sizeof(short) + 1] = shortBytes[1];
                }

                // Send audio data to the gRPC server
                try
                {
                    await call.RequestStream.WriteAsync(new GetResponseRequest
                    {
                        GetResponseData = new GetResponseData
                        {
                            AudioData = ByteString.CopyFrom(audioByteArray)
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
            }
        }

        /// <summary>
        ///     Periodically receives responses from the server and adds it to a static list in streaming NPC
        /// </summary>
        /// <param name="call">gRPC Streaming call connecting to the getResponse function</param>
        private async Task ReceiveResultFromServer(
            AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call)
        {
            while (await call.ResponseStream.MoveNext())
                try
                {
                    // Get the response from the server
                    GetResponseResponse result = call.ResponseStream.Current;

                    // Process different types of responses
                    if (result.UserQuery != null)
                        if (chatUIHandler != null)
                            // Add user query to the list
                            stringUserText.Add(result.UserQuery.TextData);

                    if (result.AudioResponse != null || result.ActionResponse != null)
                    {
                        if (result.AudioResponse != null)
                            // Log audio response data
                            Debug.Log(result.AudioResponse.TextData);

                        // Add response to the list in the active NPC
                        activeConvaiNPC.GetResponseResponses.Add(call.ResponseStream.Current);
                    }

                    // Update session ID in the active NPC
                    activeConvaiNPC.sessionID = call.ResponseStream.Current.SessionId;
                }
                catch (RpcException rpcException)
                {
                    // Handle RpcExceptions, log or throw if necessary
                    if (rpcException.StatusCode == StatusCode.Cancelled)
                        Debug.LogException(rpcException);
                    else
                        throw;
                }
        }

        /// <summary>
        ///     Adds WAV header to the audio data
        /// </summary>
        /// <param name="audioByteArray">Byte array containing audio data</param>
        /// <param name="sampleRate">Sample rate of the audio that needs to be processed</param>
        /// <returns>Byte array with added WAV header</returns>
        private byte[] AddByteToArray(byte[] audioByteArray, string sampleRate)
        {
            byte[] newArray = new byte[audioByteArray.Length + 44];
            audioByteArray.CopyTo(newArray, 44);

            int intSampleRate = Convert.ToInt32(sampleRate);

            // WAV header starts here
            newArray[0] = Convert.ToByte('R');
            newArray[1] = Convert.ToByte('I');
            newArray[2] = Convert.ToByte('F');
            newArray[3] = Convert.ToByte('F');

            byte[] newLength = BitConverter.GetBytes(audioByteArray.Length + 36);
            Buffer.BlockCopy(newLength, 0, newArray, 4, 4);

            newArray[8] = Convert.ToByte('W');
            newArray[9] = Convert.ToByte('A');
            newArray[10] = Convert.ToByte('V');
            newArray[11] = Convert.ToByte('E');

            newArray[12] = Convert.ToByte('f');
            newArray[13] = Convert.ToByte('m');
            newArray[14] = Convert.ToByte('t');
            newArray[15] = Convert.ToByte(' ');

            Buffer.BlockCopy(BitConverter.GetBytes(16), 0, newArray, 16, 4); // Chunk size
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, newArray, 20, 2); // Audio Format
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, newArray, 22, 2); // Num of channels

            Buffer.BlockCopy(BitConverter.GetBytes(intSampleRate), 0, newArray, 24, 4); // Sample rate

            Buffer.BlockCopy(BitConverter.GetBytes(intSampleRate * 2), 0, newArray, 28, 4); // Bit rate
            Buffer.BlockCopy(BitConverter.GetBytes(2), 0, newArray, 32, 2); // Block Align
            Buffer.BlockCopy(BitConverter.GetBytes(16), 0, newArray, 34, 2); // Bits per sample

            newArray[36] = Convert.ToByte('d');
            newArray[37] = Convert.ToByte('a');
            newArray[38] = Convert.ToByte('t');
            newArray[39] = Convert.ToByte('a');

            Buffer.BlockCopy(BitConverter.GetBytes(audioByteArray.Length), 0, newArray, 40,
                4); // Number of bytes of audio data
            Buffer.BlockCopy(audioByteArray, 0, newArray, 44, audioByteArray.Length);

            return newArray;
        }
    }
}