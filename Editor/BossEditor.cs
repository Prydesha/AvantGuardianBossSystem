/** ------------------------------------------------------------------------------
- Filename:   BossEditor
- Project:    MI498-SP21-IronGalaxy 
- Developers: IronGalaxy
- Created on: 2021/02/07
- Created by: shawn
------------------------------------------------------------------------------- */

using UnityEditor;
using UnityEngine;

/**
 * BossEditor.cs
 * --------------------- Description ------------------------
 * 
 * 
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/02/07       Created file.                                       shawn
 */
[CustomEditor(typeof(Boss))]
public class BossEditor : Editor
{
	private Boss _boss;

	private void OnEnable()
	{
		_boss = target as Boss;
	}

	public override void OnInspectorGUI()
	{
		// for whatever reason, these values don't update every frame ¯\_(ツ)_/¯
		base.OnInspectorGUI();
		if (Application.isPlaying)
		{
			EditorGUILayout.PrefixLabel("Current Variables");
			GUI.enabled = false;
			EditorGUILayout.IntField(new GUIContent("Current Phase"), _boss.CurrentPhaseIndex + 1);
			EditorGUILayout.FloatField(new GUIContent("Player Distance"), _boss.PlayerDistance);
			EditorGUILayout.FloatField(new GUIContent("Time Close to Player"), _boss.TimeCloseToPlayer);
			EditorGUILayout.FloatField(new GUIContent("Damage Received", "Per damage rate interval"), _boss.DamageReceivedPerInterval);
			
			EditorGUILayout.Space();
			EditorGUILayout.PrefixLabel("Utility Calculations");
			EditorGUILayout.ObjectField(new GUIContent("Current Behavior"), _boss.CurrentAction, typeof(Action), true);
			EditorGUILayout.ObjectField(new GUIContent("Current Attack"), _boss.CurrentAttack, typeof(AbstractAttack), true);
			EditorGUILayout.Space();
			EditorGUILayout.PrefixLabel("Attacks");
			foreach (var at in _boss.Attacks)
			{
				EditorGUILayout.FloatField(new GUIContent(at.name), at.lastCalculatedUtility);
			}
			EditorGUILayout.Space();
			EditorGUILayout.PrefixLabel("Actions");
			foreach (var ac in _boss.Actions)
			{
				EditorGUILayout.FloatField(new GUIContent(ac.name), ac.lastCalculatedUtility);
			}
			GUI.enabled = true;
		
		}
	}
}