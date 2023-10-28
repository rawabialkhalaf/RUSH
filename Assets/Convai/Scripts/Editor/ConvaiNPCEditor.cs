#if UNITY_EDITOR

using System;
using System.IO;
using Convai.Scripts.Utils;
using UnityEditor;
using UnityEngine;

namespace Convai.Scripts.Editor
{
    [CustomEditor(typeof(ConvaiNPC))]
    // TODO: Change the behaviour of this editor -> Store/cache state of all convai scripts whenever scene is saved. Logic to be applied is as follows:
    // As soon as scene is saved, all scripts inside Convai/Scripts folder are checked for their state. If any script is enabled, its state is stored in a file
    // The state of all the scripts attache to one game object will be stored in one meta file. The file name will be the name of the game object along with the scene name
    // When a script is loaded/attached to a game-object, meta file corresponding to that game object will be checked for the state of the scripts.
    // If any meta file exists for a game object, the state of the scripts will be restored
    public class ConvaiNPCEditor : UnityEditor.Editor
    {
        private ConvaiNPC _convaiNPC;

        private void OnEnable()
        {
            _convaiNPC = (ConvaiNPC)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!GUILayout.Button("Apply changes")) return;

            const string dialogMessage = "Do you want to apply the following changes?";

            if (!EditorUtility.DisplayDialog("Confirm Apply Changes", dialogMessage, "Yes", "No")) return;
            try
            {
                ApplyChanges();
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Success", "Changes applied successfully!", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error occurred when applying changes. Error: {ex}");
            }
        }

        private void ApplyChanges()
        {
            ApplyComponent<ConvaiActionsHandler>(_convaiNPC.includeActionsHandler);
            ApplyComponent<ConvaiLipSync>(_convaiNPC.includeLipSync);
            ApplyComponent<ConvaiHeadTracking>(_convaiNPC.includeHeadEyeTracking);
            ApplyComponent<ConvaiBlinkingHandler>(_convaiNPC.includeBlinking);
        }

        private void ApplyComponent<T>(bool includeComponent) where T : Component
        {
            T component = _convaiNPC.GetComponent<T>();
            string savedDataFileName = $"Assets/Convai/Scripts/ScriptState/{typeof(T).Name}_Data.meta";

            if (includeComponent)
            {
                if (component != null)
                {
                    // Saved state already re-added, delete saved file
                    if (File.Exists(savedDataFileName))
                        File.Delete(savedDataFileName);
                    return;
                }

                try
                {
                    component = _convaiNPC.gameObject.AddComponent<T>();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to add component of type {typeof(T).Name}, Error: {ex}");
                    return;
                }

                // If saved data exists for this component, apply it
                if (!File.Exists(savedDataFileName)) return;

                try
                {
                    string savedData = File.ReadAllText(savedDataFileName);
                    JsonUtility.FromJsonOverwrite(savedData, component);

                    // Saved state re-added, delete saved file
                    File.Delete(savedDataFileName);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to restore component data for {typeof(T).Name}, Error: {ex}");
                }
            }
            else if (component != null)
            {
                // Save the component data before destroying it
                string serializedComponentData = JsonUtility.ToJson(component);
                File.WriteAllText(savedDataFileName, serializedComponentData);

                DestroyImmediate(component);
            }
        }
    }
}

#endif