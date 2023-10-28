using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Convai.Scripts.Utils
{
    public class ConvaiTalkButtonHandler : Button
    {
        private ConvaiGRPCAPI _grpcAPI;

        protected override void Awake()
        {
            base.Awake(); // Call the base (Button) class's Awake method
            _grpcAPI = FindObjectOfType<ConvaiGRPCAPI>();
        }

        /// <summary>
        ///     Handles the event when the pointer is pressed down on the button.
        /// </summary>
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData); // Call the base class's method to handle basic button functionality
            _grpcAPI.activeConvaiNPC.StartListening(); // Start listening for voice input when the button is pressed
            Debug.Log(gameObject.name + " Was Clicked."); // Log a message about what button was clicked
        }

        /// <summary>
        ///     Handles the event when the pointer is released from the button.
        /// </summary>
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData); // Call the base class's method to handle basic button functionality
            _grpcAPI.activeConvaiNPC.StopListening(); // Stop listening when the button is released
            Debug.Log(gameObject.name + " Was Released."); // Log a message about what button was released
        }
    }
}