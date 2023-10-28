using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Service;
using UnityEngine;
using UnityEngine.Serialization;

namespace Convai.Scripts.Utils
{
    // STEP 1: Add the enum for your custom action here. 
    public enum ActionChoice
    {
        None,
        Jump,
        Crouch,
        MoveTo,
        PickUp,
        Drop
    }

    /// <summary>
    ///     DISCLAIMER: The action API is in experimental stages and can misbehave. Meanwhile, feel free to try it out and play
    ///     around with it.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Convai/Convai Actions Handler")]
    [HelpURL(
        "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/scripts-overview/convaiactionshandler.cs")]
    public class ConvaiActionsHandler : MonoBehaviour
    {
        [SerializeField] public ActionMethod[] actionMethods;

        private readonly List<ConvaiAction> _actionList = new();

        private List<string> _actions = new();
        private ConvaiNPC _currentNPC;

        private ConvaiGlobalActionSettings _globalActionSettings;

        [HideInInspector] public ActionConfig ActionConfig = new();

        // Awake is called when the script instance is being loaded
        private void Awake()
        {
            // Find the global action settings object in the scene
            _globalActionSettings = FindObjectOfType<ConvaiGlobalActionSettings>();

            // Check if the global action settings object is missing
            if (_globalActionSettings == null)
                // Log an error message to indicate missing Convai Action Settings
                Debug.LogError("Convai Action Settings missing. Please create a game object that handles actions.");

            // Check if this GameObject has a ConvaiNPC component attached
            if (GetComponent<ConvaiNPC>() != null)
                // Get a reference to the ConvaiNPC component
                _currentNPC = GetComponent<ConvaiNPC>();
        }

        // Start is called before the first frame update
        private void Start()
        {
            // Set up the action configuration

            #region Actions Setup

            // Iterate through each action method and add its name to the action configuration
            foreach (ActionMethod actionMethod in actionMethods) ActionConfig.Actions.Add(actionMethod.action);

            if (_globalActionSettings != null)
            {
                // Iterate through each character in global action settings and add them to the action configuration
                foreach (ConvaiGlobalActionSettings.Character character in _globalActionSettings.Characters)
                {
                    ActionConfig.Types.Character rpcCharacter = new()
                    {
                        Name = character.Name,
                        Bio = character.Bio
                    };

                    ActionConfig.Characters.Add(rpcCharacter);
                }

                // Iterate through each object in global action settings and add them to the action configuration
                foreach (ConvaiGlobalActionSettings.Object eachObject in _globalActionSettings.Objects)
                {
                    ActionConfig.Types.Object rpcObject = new()
                    {
                        Name = eachObject.Name,
                        Description = eachObject.Description
                    };
                    ActionConfig.Objects.Add(rpcObject);
                }
            }

            // Set the classification of the action configuration to "multistep"
            ActionConfig.Classification = "multistep";

            // Log the configured action information
            Debug.Log($"{ActionConfig}");

            #endregion

            // Start playing the action list using a coroutine
            StartCoroutine(PlayActionList());
        }

        public void ParseActions(string actionsString)
        {
            // Trim the input string to remove leading and trailing spaces
            actionsString = actionsString.Trim();

            // Split the trimmed actions string into a list of individual actions
            _actions = new List<string>(actionsString.Split(", "));

            // Iterate through each action in the list of actions
            foreach (List<string> actionWords in _actions.Select(t => new List<string>(t.Split(" "))))
                // Iterate through the words in the current action
                for (int j = 0; j < actionWords.Count; j++)
                {
                    // Separate the words into two parts: verb and object
                    string[] tempString1 = new string[j + 1];
                    string[] tempString2 = new string[actionWords.Count - j - 1];

                    Array.Copy(actionWords.ToArray(), tempString1, j + 1);
                    Array.Copy(actionWords.ToArray(), j + 1, tempString2, 0, actionWords.Count - j - 1);

                    // Check if any verb word ends with "s" and remove it
                    for (int k = 0; k < tempString1.Length; k++)
                        if (tempString1[k].EndsWith("s"))
                            tempString1[k] = tempString1[k].Remove(tempString1[k].Length - 1);

                    // Iterate through each defined Convai action
                    foreach (ActionMethod convaiAction in actionMethods)
                        // Check if the parsed verb matches any defined action
                        if (convaiAction.action.ToLower() == string.Join(" ", tempString1).ToLower())
                        {
                            GameObject tempGameObject = null;

                            // Iterate through each object in global action settings to find a match
                            foreach (ConvaiGlobalActionSettings.Object @object in _globalActionSettings.Objects)
                                if (string.Equals(@object.Name, string.Join(" ", tempString2),
                                        StringComparison.CurrentCultureIgnoreCase))
                                {
                                    Debug.Log($"Active Target: {string.Join(" ", tempString2).ToLower()}");
                                    tempGameObject = @object.gameObject;
                                }

                            // Iterate through each character in global action settings to find a match
                            foreach (ConvaiGlobalActionSettings.Character character in _globalActionSettings.Characters)
                                if (string.Equals(character.Name, string.Join(" ", tempString2),
                                        StringComparison.CurrentCultureIgnoreCase))
                                {
                                    Debug.Log($"Active Target: {string.Join(" ", tempString2).ToLower()}");
                                    tempGameObject = character.gameObject;
                                }

                            // Add the parsed action to the action list
                            _actionList.Add(new ConvaiAction(convaiAction.actionChoice, tempGameObject,
                                convaiAction.animationName));

                            break; // Break the loop as the action is found
                        }
                }
        }


        private IEnumerator PlayActionList()
        {
            while (true)
                // Check if there are actions in the action list
                if (_actionList.Count > 0)
                {
                    // Call the DoAction function for the first action in the list and wait until it's done
                    yield return DoAction(_actionList[0]);

                    // Remove the completed action from the list
                    _actionList.RemoveAt(0);
                }
                else
                {
                    // If there are no actions in the list, yield to wait for the next frame
                    yield return null;
                }
        }


        private IEnumerator DoAction(ConvaiAction action)
        {
            // STEP 2: Add the function call for your action here corresponding to your enum.
            //         Remember to yield until its return if it is a Enumerator function.

            // Use a switch statement to handle different action choices based on the ActionChoice enum
            switch (action.Verb)
            {
                case ActionChoice.MoveTo:
                    // Call the MoveTo function and yield until it's completed
                    yield return MoveTo(action.Target);
                    break;

                case ActionChoice.PickUp:
                    // Call the PickUp function and yield until it's completed
                    yield return PickUp(action.Target);
                    break;

                case ActionChoice.Drop:
                    // Call the Drop function
                    Drop(action.Target);
                    break;

                case ActionChoice.Jump:
                    // Call the Jump function
                    Jump();
                    break;

                case ActionChoice.Crouch:
                    // Call the Crouch function and yield until it's completed
                    yield return Crouch();
                    break;

                case ActionChoice.None:
                    // Call the AnimationActions function and yield until it's completed
                    yield return AnimationActions(action.Animation);
                    break;
            }

            // Yield once to ensure the coroutine advances to the next frame
            yield return null;
        }

        private IEnumerator AnimationActions(string animationName)
        {
            Debug.Log("Doing animation: " + animationName);

            // Play the animation with a fixed-time cross fade
            _currentNPC.GetComponent<Animator>().CrossFadeInFixedTime(Animator.StringToHash(animationName), 0.1f);
            yield return new WaitForSeconds(0.11f); // Wait for a short duration for the animation transition

            // Get the current animator clip information
            AnimatorClipInfo[] clipInfo = _currentNPC.GetComponent<Animator>().GetCurrentAnimatorClipInfo(0);

            float length = 0;

            foreach (AnimatorClipInfo clipInf in clipInfo) Debug.Log("Clip name: " + clipInf.clip.name);

            // Iterate through the clip info to find the length of the current animation
            foreach (AnimatorClipInfo clipInf in clipInfo)
            {
                Debug.Log("Clip name checking: " + clipInf.clip.name);

                AnimationClip clip = clipInf.clip;

                if (clip != null && clip.name == animationName) length = clip.length;
            }

            Debug.Log("Clip length: " + length);

            // Wait for the duration of the animation
            yield return new WaitForSeconds(length);

            // Transition back to the idle animation
            _currentNPC.GetComponent<Animator>().CrossFadeInFixedTime(Animator.StringToHash("Idle"), 0.1f);

            // Yield once to ensure the coroutine advances to the next frame
            yield return null;
        }

        [Serializable]
        public class ActionMethod
        {
            [FormerlySerializedAs("Action")] [SerializeField]
            public string action;

            // feels unnecessary
            // [SerializeField] public ActionType actionType;
            [SerializeField] public string animationName;
            [SerializeField] public ActionChoice actionChoice;
        }

        private class ConvaiAction
        {
            public readonly string Animation;
            public readonly GameObject Target;
            public readonly ActionChoice Verb;

            public ConvaiAction(ActionChoice verb, GameObject target, string animation)
            {
                Verb = verb;
                Target = target;
                Animation = animation;
            }
        }

        // STEP 3: Add the function for your action here.

        #region Action Implementation Methods

        private IEnumerator Crouch()
        {
            Debug.Log("Crouching!");
            _currentNPC.GetComponent<Animator>().CrossFadeInFixedTime(Animator.StringToHash("Crouch"), 0.1f);

            _currentNPC.GetComponents<CapsuleCollider>()[0].height = 1.2f;
            _currentNPC.GetComponents<CapsuleCollider>()[0].center = new Vector3(0, 0.6f, 0);

            if (_currentNPC.GetComponents<CapsuleCollider>().Length > 1)
            {
                _currentNPC.GetComponents<CapsuleCollider>()[1].height = 1.2f;
                _currentNPC.GetComponents<CapsuleCollider>()[1].center = new Vector3(0, 0.6f, 0);
            }

            yield return new WaitForSeconds(10f);
            _currentNPC.GetComponent<Animator>().CrossFadeInFixedTime(Animator.StringToHash("Idle"), 0.1f);
            yield return null;
        }

        private IEnumerator MoveTo(GameObject target)
        {
            if (target == null)
            {
                yield return null;
            }
            else
            {
                Debug.Log($"Moving to Target: {target.name}");

                _currentNPC.GetComponent<Animator>().CrossFade(Animator.StringToHash("Walking"), 0.1f);

                float moveSpeed = 0.6f;

                while (Vector3.Distance(transform.position, target.transform.position) > 1.50f)
                {
                    // Calculate the direction towards the target
                    Vector3 direction = target.transform.position - transform.position;
                    direction.y = 0f; // make sure the character stays upright
                    direction.Normalize();

                    // Rotate the character towards the target
                    transform.rotation = Quaternion.LookRotation(direction);

                    // Move the character towards the target
                    transform.position += direction * (moveSpeed * Time.deltaTime);

                    yield return null;
                }

                _currentNPC.GetComponent<Animator>().CrossFade(Animator.StringToHash("Idle"), 0.1f);
            }
        }

        private IEnumerator PickUp(GameObject target)
        {
            if (target == null)
            {
                yield return null;
            }
            else
            {
                Debug.Log($"Picking up Target: {target.name}");
                _currentNPC.GetComponent<Animator>().CrossFade(Animator.StringToHash("Picking Up"), 0.1f);

                yield return new WaitForSeconds(2.1f);
                target.transform.parent = gameObject.transform;
                // target.SetActive(false);

                target.GetComponent<MeshRenderer>().enabled = false;
                target.GetComponent<Collider>().enabled = false;

                yield return new WaitForSeconds(9.567f - 2.1f);

                _currentNPC.GetComponent<Animator>().CrossFade(Animator.StringToHash("Idle"), 0.1f);
            }
        }

        private void Drop(GameObject target)
        {
            if (target == null) return;

            Debug.Log($"Dropping Target: {target.name}");
            target.transform.parent = null;
            //target.SetActive(true);
            target.GetComponent<MeshRenderer>().enabled = true;
            target.GetComponent<Collider>().enabled = true;
        }

        private void Jump()
        {
            float jumpForce = 5f;

            GetComponent<Rigidbody>().AddForce(new Vector3(0f, jumpForce, 0f), ForceMode.Impulse);
            _currentNPC.GetComponent<Animator>().CrossFade(Animator.StringToHash("Dance"), 1);
        }

        // STEP 3: Add the function for your action here.

        #endregion
    }
}