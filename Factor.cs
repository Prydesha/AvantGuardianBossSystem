/** ------------------------------------------------------------------------------
- Filename:   Factor
- Project:    Brush Knight 
- Developers: Team BoSS
- Created on: 2021/03/02
- Created by: shawn
------------------------------------------------------------------------------- */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Factor.cs
 * --------------------- Description ------------------------
 * Represents a game element which affects the weight of an action
 * or choice made by our boss AI
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/03/02       Created file.                                       shawn
 */
[System.Serializable]
public class Factor
{
    /* PUBLIC MEMBERS */
    [Tooltip("Weight of this factor when compared to other factors of an " +
             "action (a higher relative value makes this factor more important).\n\nThis is only " +
             "applicable when there are multiple factors for a single action")]
    [Range(0.001f, 1f)] 
    public float weight = 1f;

    /* PRIVATE MEMBERS */


    /* PROPERTIES */

    // The "Properties" section is for getters/setters of 
    // your private variables (feel free to delete this comment)

    /* METHODS */
    
    
}
