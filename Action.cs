/** ------------------------------------------------------------------------------
- Filename:   Action
- Project:    MI498-SP21-IronGalaxy 
- Developers: IronGalaxy
- Created on: 2021/02/01
- Created by: shawn
------------------------------------------------------------------------------- */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Action.cs
 * --------------------- Description ------------------------
 * An action which correlates a boss behavior with a set
 * of utility factors. Use this to describe non-attacking behaviors
 * (excluding the generic attack action which does not specify an
 * actual attack)
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/02/01       Created file.                                       shawn
 */
[CreateAssetMenu(menuName = "AI/BossAction", fileName = "NewAction.asset")]
public class Action : ScriptableObject
{
    /* PUBLIC MEMBERS */
    [Tooltip("The behavior associated with this action")]
    // changes to this variable must be reflected in 'ActionEditor'
    public BossBehavior behavior = BossBehavior.Idle;
    [Tooltip("Overrides min of all factors" +
             "\n\nIf set above maximum, defaults to zero")]
    [Range(0f, 0.999f)] public float minimumUtility = 0f;
    [Tooltip("Overrides max of all factors")]
    [Range(0.001f, 1f)] public float maximumUtility = 1f;
    [Tooltip("The elements which affect our behavior")]
    public FactorSet factors = null;
    [Tooltip("As long as the utility of this attack is non-zero, this amount can be randomly " +
             "added to its calculated utility")] 
    [Range(0f, 1f)]
    public float randomUtilBonusMax = 0.01f;
	[Tooltip("After a boss decides to perform this behavior, it must continue to do so for this " +
	         "amount of time before considering any other actions")]
    [Min(0f)] public float minimumPerformTime = 0.5f;
	
	[Tooltip("If behavior == MoveSpecific, this is the location the boss will move to relative to its position when" +
	         "it entered this action")]
	[HideInInspector] public Vector3 relativeDestination = Vector3.zero;
    
    // the last calculated utility score of this action
    [HideInInspector] public float lastCalculatedUtility = 0f;
    
    /* PRIVATE MEMBERS */


    /* PROPERTIES */

    // The "Properties" section is for getters/setters of 
    // your private variables (feel free to delete this comment)

    /* METHODS */


}

[System.Serializable]
/// <summary>
/// Enumeration of all actions which a boss
/// can perform
/// </summary>
public enum BossBehavior
{
    Idle = 0,
    Attack = 1,
    MoveToPlayer = 2,
    MoveAwayFromPlayer = 3,
    MoveSpecific = 4
}

public static class BossBehaviorExtensions
{
	/// <summary>
	/// Indicates whether a transition from a calling behavior to a new behavior
	/// is valid
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <returns></returns>
	public static bool CanTransitionTo(this BossBehavior from, BossBehavior to)
	{
		switch (from)
		{
			case BossBehavior.Attack:
				// TODO: when states become more complex, be sure
				// that the attack state can transition to itself
				return true;
			default:
				return from != to;
		}
	}
}
