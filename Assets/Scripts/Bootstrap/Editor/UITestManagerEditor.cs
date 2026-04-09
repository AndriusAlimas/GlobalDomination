using GlobalDomination.Managers;
using UnityEditor;
using UnityEngine;

namespace GlobalDomination.Editor
{
    [CustomEditor(typeof(UITestManager))]
    public class UITestManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty currentPlayerText;
        private SerializedProperty currentPlayerFlagImage;
        private SerializedProperty endTurnButton;
        private SerializedProperty endTurnButtonText;
        private SerializedProperty backgroundMusic;
        private SerializedProperty autoInitializeGame;
        private SerializedProperty showStartupStatRollReveal;
        private SerializedProperty devShowSkipStartupButton;
        private SerializedProperty startupStatSpinDuration;
        private SerializedProperty startupAutoNextSeconds;
        private SerializedProperty buildingRollFailToastSeconds;
        private SerializedProperty devStartupPlayer1;
        private SerializedProperty devStartupPlayer2;

        private void OnEnable()
        {
            currentPlayerText = serializedObject.FindProperty("currentPlayerText");
            currentPlayerFlagImage = serializedObject.FindProperty("currentPlayerFlagImage");
            endTurnButton = serializedObject.FindProperty("endTurnButton");
            endTurnButtonText = serializedObject.FindProperty("endTurnButtonText");
            backgroundMusic = serializedObject.FindProperty("backgroundMusic");
            autoInitializeGame = serializedObject.FindProperty("autoInitializeGame");
            showStartupStatRollReveal = serializedObject.FindProperty("showStartupStatRollReveal");
            devShowSkipStartupButton = serializedObject.FindProperty("devShowSkipStartupButton");
            startupStatSpinDuration = serializedObject.FindProperty("startupStatSpinDuration");
            startupAutoNextSeconds = serializedObject.FindProperty("startupAutoNextSeconds");
            buildingRollFailToastSeconds = serializedObject.FindProperty("buildingRollFailToastSeconds");
            devStartupPlayer1 = serializedObject.FindProperty("devStartupPlayer1");
            devStartupPlayer2 = serializedObject.FindProperty("devStartupPlayer2");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("UI References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(currentPlayerText);
            EditorGUILayout.PropertyField(currentPlayerFlagImage);
            EditorGUILayout.PropertyField(endTurnButton);
            EditorGUILayout.PropertyField(endTurnButtonText);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(backgroundMusic);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoInitializeGame);
            EditorGUILayout.PropertyField(showStartupStatRollReveal);
            EditorGUILayout.PropertyField(devShowSkipStartupButton);

            if (devShowSkipStartupButton.boolValue)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(
                    "Dev skip: capital stats below replace dice rolls for the test game (Player 1 / Player 2). "
                    + "If Extra Startup Buildings is empty, the normal building dice roll still runs.",
                    MessageType.Info);
                EditorGUILayout.PropertyField(
                    devStartupPlayer1,
                    new GUIContent("Player 1 (test) capital", "Matches InitializeTestGame order."));
                EditorGUILayout.PropertyField(
                    devStartupPlayer2,
                    new GUIContent("Player 2 (test) capital", "Matches InitializeTestGame order."));
            }

            EditorGUILayout.PropertyField(startupStatSpinDuration);
            EditorGUILayout.PropertyField(startupAutoNextSeconds);
            EditorGUILayout.PropertyField(buildingRollFailToastSeconds);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
