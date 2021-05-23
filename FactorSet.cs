/** ------------------------------------------------------------------------------
- Filename:   FactorSet
- Project:    Brush Knight 
- Developers: Team BoSS
- Created on: 2021/02/25
- Created by: shawn
------------------------------------------------------------------------------- */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/** FactorSet.cs
 * --------------------- Description ------------------------
 * Represents a list of utility factors, used primarily for boss attacks
 *
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/02/25       Created file.                                       shawn
 */
[CreateAssetMenu(menuName = "AI/FactorSet", fileName = "NewFactorSet.asset")]
public class FactorSet : ScriptableObject
{
    /* PUBLIC MEMBERS */

    [Tooltip("list of factors which contribute to the utility of this factor set \n\n" +
             "The weights of each of these summed together with the weights of all Color Factors on this " +
             "factor set should total to exactly 1")]
    [FormerlySerializedAs("Factors")] 
    public List<VariableFactor> VariableFactors = new List<VariableFactor>();
    [Tooltip("list of color based factors which contribute to the utility of this factor set \n\n" +
             "The weights of each of these summed together with the weights of all Variable Factors on this " +
             "factor set should total to exactly 1")]
    public List<ColorFactor> ColorFactors = new List<ColorFactor>();

    /* PRIVATE MEMBERS */


    /* PROPERTIES */

    /// <summary>
    /// The total number of factors that this factor set contains
    /// </summary>
    public int TotalFactors => VariableFactors.Count + ColorFactors.Count;

    /* METHODS */


}
