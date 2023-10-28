using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Convai.Scripts.Utils;
using Newtonsoft.Json;
using System;

namespace Convai.Scripts.Utils
{
    public class ConvaiLipSync : MonoBehaviour
    {
        public enum LipSyncType
        {
            None, //Default Value
            FlappyMouth,
            Reallusion
        }
        private class BlendShape
        {
            public string name { get; set; }
            public double score { get; set; }
        }

        private class FrameData
        {
            public int FrameIndex { get; set; }
            public List<BlendShape> BlendShapes { get; set; }
        }

        private class ReallusionBlendShapes
        {
            public string name { get; set; }
            public int index { get; set; }

            public ReallusionBlendShapes(string name, int index)
            {
                this.name = name;
                this.index = index;
            }
        }

        public LipSyncType convaiLipSyncType;
        private int currentFrameIndex = 0;
        private int currentIndex = 0;
        public SkinnedMeshRenderer Head;
        private List<FrameData> frames;
        public GameObject UpperJaw;
        public GameObject LowerJaw;

        private Dictionary<string, ReallusionBlendShapes> ReallusionBlendShape = new Dictionary<string, ReallusionBlendShapes>
            {
                {"_neutral", null},
                {"browDownLeft", new ReallusionBlendShapes("Brow_Drop_L", 12)},
                { "browDownRight", new ReallusionBlendShapes("Brow_Drop_R", 13)},
                { "browInnerUp", null},
                { "browOuterUpLeft", new ReallusionBlendShapes("Brow_Raise_Outer_L", 10)},
                { "browOuterUpRight", new ReallusionBlendShapes("Brow_Raise_Outer_R", 11)},
                { "cheekPuff", null},
                { "cheekSquintLeft", new ReallusionBlendShapes("Cheek_Suck_L", 62)},
                { "cheekSquintRight", new ReallusionBlendShapes("Cheek_Suck_R", 63)},
                { "eyeBlinkLeft", new ReallusionBlendShapes("Eye_Blink_L", 16)},
                { "eyeBlinkRight", new ReallusionBlendShapes("Eye_Blink_R", 17)},
                { "eyeLookDownLeft", new ReallusionBlendShapes("Eye_L_Look_Down", 28)},
                { "eyeLookDownRight", new ReallusionBlendShapes("Eye_R_Look_Down", 29)},
                { "eyeLookInLeft", null},
                { "eyeLookInRight", null},
                { "eyeLookOutLeft", null},
                { "eyeLookOutRight", null},
                { "eyeLookUpLeft", null},
                { "eyeLookUpRight", null},
                { "eyeSquintLeft", null},
                { "eyeSquintRight", null},
                { "eyeWideLeft", null},
                { "eyeWideRight", null},
                { "jawForward", new ReallusionBlendShapes("Jaw_Forward", 128)},
                { "jawLeft", new ReallusionBlendShapes("Jaw_L", 130)},
                { "jawOpen", new ReallusionBlendShapes("Jaw_Open", 127)},
                { "jawRight", new ReallusionBlendShapes("Jaw_R", 131)},
                { "mouthClose", new ReallusionBlendShapes("Mouth_Close", 123)},
                { "mouthDimpleLeft", new ReallusionBlendShapes("Mouth_Dimple_L", 74)},
                { "mouthDimpleRight", new ReallusionBlendShapes("Mouth_Dimple_R", 75)},
                { "mouthFrownLeft", new ReallusionBlendShapes("Mouth_Frown_L", 70)},
                { "mouthFrownRight", new ReallusionBlendShapes("Mouth_Frown_R", 71)},
                { "mouthFunnel", new ReallusionBlendShapes("Funnel", 71)},
                { "mouthLeft", new ReallusionBlendShapes("Mouth_L", 108)},
                { "mouthLowerDownLeft", new ReallusionBlendShapes("Mouth_Down_Lower_L", 120)},
                { "mouthLowerDownRight", new ReallusionBlendShapes("Mouth_Down_Lower_R", 121)},
                { "mouthPressLeft", new ReallusionBlendShapes("Mouth_Press_L", 76)},
                { "mouthPressRight", new ReallusionBlendShapes("Mouth_Press_R", 77)},
                { "mouthPucker", new ReallusionBlendShapes("Mouth_Pucker", 82)},
                { "mouthRight", new ReallusionBlendShapes("Mouth_R", 109)},
                { "mouthRollLower", new ReallusionBlendShapes("Mouth_R", 92)},
                { "mouthRollUpper", new ReallusionBlendShapes("Mouth_R", 90)},
                { "mouthShrugLower", new ReallusionBlendShapes("Mouth_Shrug_Lower", 115)},
                { "mouthShrugUpper", new ReallusionBlendShapes("Mouth_Shrug_Upper", 114)},
                { "mouthSmileLeft", new ReallusionBlendShapes("Mouth_Smile_L", 66)},
                { "mouthSmileRight", new ReallusionBlendShapes("Mouth_Smile_R", 67)},
                { "mouthStretchLeft", new ReallusionBlendShapes("Mouth_Stretch_L", 72)},
                { "mouthStretchRight", new ReallusionBlendShapes("Mouth_Stretch_R", 73)},
                { "mouthUpperUpLeft", new ReallusionBlendShapes("Mouth_Up_Upper_L", 118)},
                { "mouthUpperUpRight", new ReallusionBlendShapes("Mouth_Up_Upper_R", 119)},
                { "noseSneerLeft", new ReallusionBlendShapes("Nose_Sneer_L", 44)},
                { "noseSneerRight", new ReallusionBlendShapes("Nose_Sneer_R", 45)}
            };

        // Start function added so that we can disable the script as needed from the editor without having to remove the component
        private void Start()
        {
            
        }

        /// <summary>
        /// Synchronizes the character's lip movements to the provided audio response.
        /// </summary>
        /// <param name="responseAudio">The audio response containing both the audio clip and facial data for lip syncing.</param>
        public void LipSyncCharacter(ConvaiNPC.ResponseAudio responseAudio)
        {
            if (convaiLipSyncType == LipSyncType.Reallusion)
            {
                if (responseAudio.FaceData != null)
                {
                    if (responseAudio.FaceData != "")
                    {
                        // Deserialize and store the face data from the audio response into a list of frame data.
                        frames = JsonConvert.DeserializeObject<List<FrameData>>(responseAudio.FaceData);

                        // Calculate the total length of the audio clip in seconds.
                        float lengthOfAudio = responseAudio.AudioClip.length;

                        // Determine the time each frame should be displayed based on the total length of the audio and the last frame's index.
                        float oneFramePlayTime = lengthOfAudio / frames[^1].FrameIndex;

                        // Cancel any existing invocations of "ChangeBlendShape" to ensure no overlap.
                        CancelInvoke(nameof(ChangeReallusionCharacterBlendShape));

                        // Schedule the "ChangeBlendShape" method to run at intervals determined by the calculated frame play time.
                        InvokeRepeating(nameof(ChangeReallusionCharacterBlendShape), 0f, oneFramePlayTime);
                    }
                }
            }
        }

        private void ChangeReallusionCharacterBlendShape()
        {
            if (frames != null && (currentFrameIndex <= frames[^1].FrameIndex && currentIndex < frames.Count))
            {
                if (currentFrameIndex == frames[currentIndex].FrameIndex)
                {
                    foreach (BlendShape currentBlendShape in frames[currentIndex].BlendShapes)
                    {
                        if (ReallusionBlendShape.TryGetValue(currentBlendShape.name, out ReallusionBlendShapes reallusionBlendShapesData) && ReallusionBlendShape[currentBlendShape.name] != null)
                        {

                            if (currentBlendShape.name == "mouthFunnel")
                            {
                                for (int i = 86; i <= 89; i++)
                                {
                                    float targetValue = Math.Min((float)currentBlendShape.score * 700, 100);
                                    float currentValue = Head.GetBlendShapeWeight(i);
                                    float newValue = Mathf.Lerp(currentValue, targetValue, 0.6f);
                                    Head.SetBlendShapeWeight(i, newValue);
                                }
                            }

                            else if (currentBlendShape.name == "mouthPucker")
                            {
                                for (int i = 82; i <= 85; i++)
                                {
                                    float targetValue = Math.Min((float)currentBlendShape.score * 800, 100);
                                    float currentValue = Head.GetBlendShapeWeight(i);
                                    float newValue = Mathf.Lerp(currentValue, targetValue, 0.2f);
                                    Head.SetBlendShapeWeight(i, newValue);
                                }
                            }

                            else if (currentBlendShape.name == "mouthRollLower")
                            {
                                for (int i = 92; i <= 93; i++)
                                {
                                    float targetValue = Math.Min((float)currentBlendShape.score * 1000, 100);
                                    float currentValue = Head.GetBlendShapeWeight(i);
                                    float newValue = Mathf.Lerp(currentValue, targetValue, 0.8f);
                                    Head.SetBlendShapeWeight(i, newValue);
                                }
                            }

                            else if (currentBlendShape.name == "mouthRollUpper")
                            {
                                for (int i = 90; i <= 91; i++)
                                {
                                    float targetValue = Math.Min((float)currentBlendShape.score * 1000, 100);
                                    float currentValue = Head.GetBlendShapeWeight(i);
                                    float newValue = Mathf.Lerp(currentValue, targetValue, 0.6f);
                                    Head.SetBlendShapeWeight(i, newValue);
                                }
                            }

                            else if (currentBlendShape.name == "mouthClose")
                            {
                                float currentValue = Head.GetBlendShapeWeight(reallusionBlendShapesData.index);
                                float newValue = Mathf.Lerp(currentValue, (float)(currentBlendShape.score - 1) * 60, 0.6f);
                                Vector3 upperJawCurrentRotation = UpperJaw.transform.localEulerAngles;
                                Vector3 lowerJawCurrentRotation = LowerJaw.transform.localEulerAngles;
                                UpperJaw.transform.localEulerAngles = new Vector3(-180, 0, (float)(currentBlendShape.score - 1) * 7);
                                LowerJaw.transform.localEulerAngles = new Vector3(-180, 0, (float)(1 - currentBlendShape.score) * 7);
                            }

                            else if (currentBlendShape.name == "jawOpen")
                            {
                                float currentValue = Head.GetBlendShapeWeight(123);
                                float targetValue = Math.Max((float)(-currentBlendShape.score * 60), -100);
                                float newValue = Mathf.Lerp(currentValue, targetValue, 0.5f);
                                Head.SetBlendShapeWeight(123, newValue);
                            }

                            else if (currentBlendShape.name.Contains("eye") || currentBlendShape.name.Contains("brow"))
                            {
                                //Avoiding all eye and brow animations
                            }

                            else if (currentBlendShape.name.Contains("Smile"))
                            {
                                float targetValue = Math.Min((float)currentBlendShape.score * 100, 100);
                                targetValue = Math.Min((float)targetValue, 100);
                                float currentValue = Head.GetBlendShapeWeight(reallusionBlendShapesData.index);
                                float newValue = Mathf.Lerp(currentValue, targetValue, 0.6f);
                                Head.SetBlendShapeWeight(reallusionBlendShapesData.index, newValue);
                            }

                            else
                            {
                                float targetValue = Math.Min((float)currentBlendShape.score * 1000, 100);
                                float currentValue = Head.GetBlendShapeWeight(reallusionBlendShapesData.index);
                                float newValue = Mathf.Lerp(currentValue, targetValue, 0.6f);
                                Head.SetBlendShapeWeight(reallusionBlendShapesData.index, newValue);
                            }
                        }
                    }
                    currentIndex++;
                }
                currentFrameIndex++;
            }
        }

        /// <summary>
        /// Resets the character's lips to their default state by:
        /// 1. Resetting all blend shapes of the character's head to a default weight.
        /// 2. Reinitializing tracking counters and clearing frame references.
        /// 3. Restoring the upper and lower jaws to their default rotations.
        /// 4. Stopping any scheduled invocations of "ChangeBlendShape" method.
        /// </summary>
        public void ResetCharacterLips()
        {
            if (convaiLipSyncType == LipSyncType.Reallusion)
            {
                // Set all blend shapes on the head mesh to their default weights.
                for (int i = 0; i < Head.sharedMesh.blendShapeCount; i++)
                {
                    Head.SetBlendShapeWeight(i, 0);
                }

                // Reinitialize tracking counters and clear frame references.
                currentFrameIndex = 0;
                currentIndex = 0;
                frames = null;

                // Set the upper and lower jaws back to their default rotation angles.
                UpperJaw.transform.localEulerAngles = new Vector3(180, 0, 0);
                LowerJaw.transform.localEulerAngles = new Vector3(180, 0, 0);

                // If there's a scheduled invocation of "ChangeBlendShape", cancel it.
                CancelInvoke(nameof(ChangeReallusionCharacterBlendShape));
            }
        }
    }
}