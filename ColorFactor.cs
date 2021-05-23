/** ------------------------------------------------------------------------------
- Filename:   ColorFactor
- Project:    Brush Knight 
- Developers: Team BoSS
- Created on: 2021/03/02
- Created by: shawn
------------------------------------------------------------------------------- */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** ColorFactor.cs
 * --------------------- Description ------------------------
 * A type of Factor which is correlated to a paint color
 * (Currently only for color currently applied to boss)
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/03/02       Created file.                                       shawn
 */
[System.Serializable]
public class ColorFactor : Factor
{
    /* PUBLIC MEMBERS */
    [Tooltip("The effecting color")]
    public PaintColorAuthoritative.PaintColorEnum Color = PaintColorAuthoritative.PaintColorEnum.Red;

    [Tooltip("Value of this utility factor when the associated boss has this paint effect on it")]
    [Range(0f, 1f)]
    public float ActiveUtility = 1f;
    [Tooltip("Value of this utility factor when the associated boss does not have this paint effect on it")]
    [Range(0f, 1f)]
    public float NotActiveUtility = 0.1f;
    /* PRIVATE MEMBERS */


    /* PROPERTIES */

    /* METHODS */

}
