using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SpaceSeries.Core.Player;
using SpaceSeries.Core.Dialogue;

namespace SpaceSeries.Core.Dialogue.Editor
{
    /// <summary>
    /// Editor utility that links DialogueRunner with the player astronaut controller
    /// and sets up an interactive crewmate NPC in the active scene.
    /// </summary>
    public static class DialogueCharacterConnector
    {
        private const string HADLEY_CONVO_PATH = "Assets/_Project/Apollo15/Data/Dialogues/Conversation_Crewmate_HadleyPlain.asset";
        private const string DIALOGUE_CHANNEL_PATH = "Assets/_Project/Apollo15/Data/Experiments/Channels/DialogueChannelSO.asset";
        private const string ASTRONAUT_MODEL_PATH = "Assets/Stylized_Astronaut/Character/Astronaut.fbx";

        [MenuItem("Tools/SpaceSeries/Connect Dialogue to Character")]
        public static void ConnectDialogueToCharacter()
        {
            // 1. Locate DialogueRunner and AstronautController
            DialogueRunner dialogueRunner = Object.FindFirstObjectByType<DialogueRunner>();
            AstronautController astronaut = Object.FindFirstObjectByType<AstronautController>();

            if (dialogueRunner == null)
            {
                Debug.LogError("[DialogueConnector] No DialogueRunner found in the active scene.");
                return;
            }

            if (astronaut == null)
            {
                Debug.LogError("[DialogueConnector] No AstronautController found in the active scene.");
                return;
            }

            // 2. Link AstronautController to DialogueRunner
            SerializedObject runnerSO = new SerializedObject(dialogueRunner);
            SerializedProperty astronautProp = runnerSO.FindProperty("_astronautController");
            astronautProp.objectReferenceValue = astronaut;
            runnerSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(dialogueRunner);
            Debug.Log($"[DialogueConnector] Linked DialogueRunner '{dialogueRunner.name}' to AstronautController on '{astronaut.name}'.");

            // 3. Find or Create Crewmate NPC (Jim Irwin)
            GameObject npc = GameObject.Find("NPC_JimIrwin");
            if (npc == null)
            {
                npc = new GameObject("NPC_JimIrwin");
                Vector3 spawnPos = astronaut.transform.position + astronaut.transform.forward * 2.5f + astronaut.transform.right * 1.5f;
                npc.transform.position = spawnPos;
                npc.transform.rotation = Quaternion.LookRotation(-astronaut.transform.forward);

                // Add Collider
                CapsuleCollider collider = npc.AddComponent<CapsuleCollider>();
                collider.radius = 0.45f;
                collider.height = 1.8f;
                collider.center = new Vector3(0f, 0.9f, 0f);

                // Instantiate visual astronaut mesh if available
                GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ASTRONAUT_MODEL_PATH);
                if (modelPrefab != null)
                {
                    GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, npc.transform);
                    modelInstance.name = "Astronaut_Visual";
                    modelInstance.transform.localPosition = Vector3.zero;
                    modelInstance.transform.localRotation = Quaternion.identity;
                }

                Undo.RegisterCreatedObjectUndo(npc, "Create Jim Irwin NPC");
            }

            // 4. Configure DialogueTrigger on NPC
            DialogueTrigger trigger = npc.GetComponent<DialogueTrigger>();
            if (trigger == null)
            {
                trigger = npc.AddComponent<DialogueTrigger>();
            }

            DialogueSequenceSO sequence = AssetDatabase.LoadAssetAtPath<DialogueSequenceSO>(HADLEY_CONVO_PATH);
            DialogueChannelSO channel = AssetDatabase.LoadAssetAtPath<DialogueChannelSO>(DIALOGUE_CHANNEL_PATH);

            SerializedObject triggerSO = new SerializedObject(trigger);
            triggerSO.FindProperty("_dialogueSequence").objectReferenceValue = sequence;
            triggerSO.FindProperty("_dialogueChannel").objectReferenceValue = channel;
            triggerSO.FindProperty("_promptText").stringValue = "Jim Irwin";
            triggerSO.FindProperty("_actionVerb").stringValue = "Talk to";
            triggerSO.FindProperty("_interactionDistance").floatValue = 3.0f;
            triggerSO.FindProperty("_oneShotOnly").boolValue = false;
            triggerSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(trigger);

            // 5. Mark Scene Dirty and Save
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[DialogueConnector] Successfully connected dialogue with the character and configured NPC Jim Irwin!");
        }
    }
}
