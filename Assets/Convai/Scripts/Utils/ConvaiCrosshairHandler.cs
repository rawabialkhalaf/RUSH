using UnityEngine;

namespace Convai.Scripts.Utils
{
    /// <summary>
    ///     Handles the crosshair behavior for the Convai application.
    ///     It can detect which Convai game object the player's crosshair is currently looking at.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Convai/Crosshair Handler")]
    [HelpURL(
        "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/scripts-overview/convaicrosshairhandler.cs")]
    public class ConvaiCrosshairHandler : MonoBehaviour
    {
        // Cached references
        private Camera _camera;
        private ConvaiGlobalActionSettings _globalActionSettings;

        // Initialization
        private void Awake()
        {
            // Find necessary components in the scene
            _globalActionSettings = FindObjectOfType<ConvaiGlobalActionSettings>();
            _camera = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<Camera>();
        }

        /// <summary>
        ///     Finds the reference object that the player's crosshair is currently looking at.
        /// </summary>
        /// <returns>A reference string of the interactable object or character, "None" if no valid hit.</returns>
        public string FindPlayerReferenceObject()
        {
            if (_globalActionSettings == null || _camera == null) return "None";

            // Check if the player's crosshair is looking at a valid object
            return Physics.Raycast(_camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)), out RaycastHit hit)
                ? FindInteractableReference(hit.transform.gameObject)
                : "None";
        }

        /// <summary>
        ///     Helper method to find interactable object or character reference.
        /// </summary>
        /// <param name="lookingAtGameObject">The GameObject being looked at by the crosshair.</param>
        /// <returns>A reference string of the interactable object or character, "None" if not found.</returns>
        private string FindInteractableReference(Object lookingAtGameObject)
        {
            // Search for a matching reference in all objects
            foreach (ConvaiGlobalActionSettings.Object eachObject in _globalActionSettings.Objects)
                if (lookingAtGameObject == eachObject.gameObject)
                    return eachObject.Name;

            // Search for a matching reference in all characters
            foreach (ConvaiGlobalActionSettings.Character eachCharacter in _globalActionSettings.Characters)
                if (lookingAtGameObject == eachCharacter.gameObject)
                    return eachCharacter.Name;

            return "None";
        }
    }
}