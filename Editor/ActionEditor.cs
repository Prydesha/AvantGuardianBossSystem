/** ------------------------------------------------------------------------------
- Filename:   ActionEditor
- Project:    MI498-SP21-IronGalaxy 
- Developers: IronGalaxy
- Created on: 2021/02/07
- Created by: shawn
------------------------------------------------------------------------------- */

using UnityEditor;
using UnityEngine;

/**
 * ActionEditor.cs
 * --------------------- Description ------------------------
 * Custom editor for boss action scripts
 * 
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/02/07       Created file.                                       shawn
 */
[CustomEditor(typeof(Action))]
public class ActionEditor : Editor
{
	private Action _action;
	private SerializedObject _serializedTarget;
	private SerializedProperty _behaviorType;
	private SerializedProperty _relativeDestination;

	private void OnEnable()
	{
		_action = target as Action;
		_serializedTarget = new SerializedObject(_action);

		_behaviorType = _serializedTarget.FindProperty("behavior");
		_relativeDestination = _serializedTarget.FindProperty("relativeDestination");
	}

	public override void OnInspectorGUI()
	{
		_serializedTarget.Update();
		switch (_action.behavior)
		{
			case BossBehavior.Attack:
				EditorGUILayout.PropertyField(_behaviorType, new GUIContent("Behavior"));

				EditorGUILayout.LabelField("The utility of the attack action is based on the highest \n" +
				                           "utility of all available attacks to a boss", GUILayout.Height(40f));
				break;
			case BossBehavior.MoveSpecific:
				base.OnInspectorGUI();
				EditorGUILayout.Space();
				EditorGUILayout.PropertyField(_relativeDestination);
				break;
			default:
				base.OnInspectorGUI();
				break;
		}
		// ALWAYS END WITH THIS CALL
		_serializedTarget.ApplyModifiedProperties();
	}
}