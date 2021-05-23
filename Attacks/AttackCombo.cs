/** ------------------------------------------------------------------------------
- Filename:   AttackCombo
- Project:    Brush Knight 
- Developers: Team BoSS
- Created on: 2021/03/03
- Created by: shawn
------------------------------------------------------------------------------- */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

/** AttackCombo.cs
 * --------------------- Description ------------------------
 * A type of attack which represents a series of consecutive attacks
 * (does not actually attack on its own, relying on it's attack series)
 *
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/03/03       Created file.                                       shawn
 */
public class AttackCombo : AbstractAttack
{
    /* PUBLIC MEMBERS */

    [Space]
    [Tooltip("The ordered list of references to attacks which will be performed by this combo")]
    [SerializeField] private List<AbstractAttack> _orderedCombo = new List<AbstractAttack>();
    [Tooltip("If within the size range of Ordered Combo (starting from 0), the ordered combo " +
             "will randomly stop at one of the attack indices " +
             "within this range [minimumInclusive, maximumInclusive]")]
    [SerializeField] private Vector2Int _randomStopRange = Vector2Int.one;
	[Tooltip("Time in seconds that it takes for the boss performing this attack " +
	         "to recover after it has finished the combo")]
    [Min(0f)]
    [SerializeField] private float _cooldownTime = 2f;

	[SerializeField] private string _cooldownAnimation = "";
	[Tooltip("Damage multiplier to apply to the boss' health when they are stunned from performing this attack.\n" +
	         "Values above one make the boss take more damage, values below one make the boss take " +
	         "less damage.")]
	[SerializeField]
	[Min(0f)]
	protected float _stunDamageMult = 1.1f;

    /* PRIVATE MEMBERS */


    /* PROPERTIES */

    // The "Properties" section is for getters/setters of 
    // your private variables (feel free to delete this comment)

    /* METHODS */


    protected override IEnumerator StartupFrames()
    {
	    yield return StartCoroutine(WaitForFrames(_startupFrames));
    }

    protected override IEnumerator ActiveFrames()
    {
	    if (_orderedCombo.Count < 1)
	    {
		    yield break;
	    }
		
	    int randomStopIndex = Random.Range(_randomStopRange.x, _randomStopRange.y + 1);
	    int i = 0;
	    // perform combo
	    foreach (var currentAttack in _orderedCombo)
	    {
		    if (i == randomStopIndex)
		    {
			    yield break;
		    }
		    currentAttack.PerformAttack(_boss, (_player.transform.position - 
		                                        _boss.transform.position), _player);
		    while (currentAttack.Active)
		    {
			    yield return null;
		    }
		    i++;
	    }
    }

    protected override IEnumerator RecoveryFrames()
    {
	    _boss.SetDebugBehaviorText(gameObject.name + " cooldown");
	    //yield return StartCoroutine(WaitForFrames(_recoveryFrames));
	    int damIndex = _boss.Health.AddDamageMultiplier(_stunDamageMult);
	    _boss.Animator.SetBool(_cooldownAnimation, true);
	    yield return new WaitForSeconds(_cooldownTime);
	    _boss.Animator.SetBool(_cooldownAnimation, false);
	    _boss.Health.RemoveDamageMultiplier(damIndex);
	    yield return StartCoroutine(WaitForIdleTrigger());
    }

    public override void TriggerActiveFrames()
    {
	    base.TriggerActiveFrames();
	    foreach (var at in _orderedCombo)
	    {
		    at.TriggerActiveFrames();
	    }
    }

    protected override void AwakeExtend()
    {
	    for (int i = _orderedCombo.Count - 1; i > 0; i--)
	    {
		    if (_orderedCombo[i] == null || _orderedCombo[i] == this)
		    {
			    _orderedCombo.RemoveAt(i);
		    }
	    }
    }
}
