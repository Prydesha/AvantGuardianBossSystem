/** ------------------------------------------------------------------------------
- Filename:   BossPhase
- Project:    Brush Knight 
- Developers: Team BoSS
- Created on: 2021/02/25
- Created by: shawn
------------------------------------------------------------------------------- */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** BossPhase.cs
 * --------------------- Description ------------------------
 * Represents a collection of actions/attacks which are
 * available to a boss within a certain range of hp
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/02/25       Created file.                                       shawn
 */
[CreateAssetMenu(menuName = "AI/BossPhase", fileName = "NewPhase.asset")]
public class BossPhase : ScriptableObject
{
    /* PUBLIC MEMBERS */
	
    [Tooltip("Once the associated boss's health is reduced to this " +
             "percentage of its maximum health, the boss will leave this phase and enter its" +
             "next phase (this value also determines the ordering of phases in code)")]
    [Range(0f, 0.99f)]
    public float healthLowerBound = 0.75f;
    [Tooltip("The actions performable by this boss")]
    public List<Action> actions = new List<Action>();

    /* PRIVATE MEMBERS */


    /* PROPERTIES */

    
    /* METHODS */

    
}
