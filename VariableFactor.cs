/** ------------------------------------------------------------------------------
- Filename:   VariableFactor
- Project:    MI498-SP21-IronGalaxy 
- Developers: IronGalaxy
- Created on: 2021/02/01
- Created by: shawn
------------------------------------------------------------------------------- */

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

/** VariableFactor.cs
 * --------------------- Description ------------------------
 * A type of Factor which is related to a boss variable
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/02/01       Created file.                                       shawn
 */
[System.Serializable]
public class VariableFactor : Factor
{
    /* PUBLIC MEMBERS */
    [Tooltip("Specifies what data value this factor is calculated with.\n" +
             "left end of curve <-> right end of curve\n\n" +
             "PlayerDistance:\nclose <-> far\n\n" + 
             "MyHealth:\nno health <-> full health\n\n" +
             "TimeCloseToPlayer:\nhas not been around player at all <-> " +
             "has been around the player for an extended period of time\n\n" +
             "DamageReceivedRate:\nno damage received recently <-> a lot of damage received recently\n\n" + 
             "PlayerDirection:\nsee Utility Function Tooltip for more explanation\n\n" + 
             "TimeTaken:\ntime in action = 0 <-> time in action = Max Value\n\n" +
             "AttackRate:\nnot attacking at all <-> attacking as much as possible")]
    public FactorType type = FactorType.PlayerDistance;
    
    [Tooltip("Equation used to determine the utility of this factor. Keep all values within 0 and 1 " +
             "(including timeStart and timeEnd).\n\n" +
             "x = value corresponding to type, \n" +
             "y = utility of this action \n\n" +
             "For PlayerDirection: \n" +
             "x of zero == left \n" +
             "x of 0.25 == down \t etc...")]
    public AnimationCurve utilityFunction = AnimationCurve.Linear(0, 0, 1, 1);

    [Tooltip("Used to indicate the tracked variable value which correlates to an x value of 1 on this " +
             "factor's utilityFunction.\n\n" +
             "Only applicable for the following factor types:\n" +
             "Time Taken")]
    [Min(1f)] 
    public float maxValue = 1f;

    /* PRIVATE MEMBERS */


    /* PROPERTIES */

    // The "Properties" section is for getters/setters of 
    // your private variables (feel free to delete this comment)

    /* METHODS */

}

[System.Serializable]
public enum FactorType
{
    PlayerDistance = 0,
    // PlayerHealth = 1,
    MyHealth = 2,
    // MyStamina = 3,
    TimeCloseToPlayer = 4,
    DamageReceivedRate = 5,
    PlayerDirection = 6,
    TimeTaken = 7,
    AttackRate = 8,
}