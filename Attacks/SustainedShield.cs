/** ------------------------------------------------------------------------------
- Filename:   SustainedShield
- Project:    Avant Guardian 
- Developers: Team BoSS
- Created on: 2021/03/16
- Created by: shawn
------------------------------------------------------------------------------- */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** SustainedShield.cs
 * --------------------- Description ------------------------
 * A type of shield attack which stays shielded until a certain damage
 * threshold is achieved
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/03/16       Created file.                                       shawn
 */
public class SustainedShield : Shield
{
    /* PUBLIC MEMBERS */

    [Space]
    [Tooltip("How much damage this shield blocks until it is deactivated")]
    [Min(1)]
    [SerializeField] private int _damageThreshold = 10;

    [Tooltip("When the damage threshold is achieved, this amount of damage is inflicted upon the boss")] 
    [Min(1)] [SerializeField]
    private int _shieldBreakSelfDamage = 2;

    [SerializeField] private GameObject _crack = null;

    [Tooltip("Maximum time in seconds to wait for the player to destroy this sustained shield")] [Min(1f)] 
    private float _maximumHitWaitTime = 60f;

    /* PRIVATE MEMBERS */

    private int _sustainedDamage = 0;
    private static readonly string RoarString = "roar";
    private static readonly int Roar = Animator.StringToHash(RoarString);

    /* PROPERTIES */
    

    /* METHODS */

    protected override IEnumerator ActiveFrames()
    {
	    _boss.Health.DamageImmune = true;

	    _sustainedDamage = 0;
	    var timeWaiting = 0f;
	    while (_sustainedDamage < _damageThreshold && timeWaiting < _maximumHitWaitTime)
	    {
		    timeWaiting += Time.deltaTime;
		    yield return null;
	    }
	    _boss.Health.DamageImmune = false;
	    _boss.Health.Damage(_shieldBreakSelfDamage);
	    if (_crack)
	    {
		    _crack.SetActive(true);
	    }
	    AudioManager.Play("ShellCrack");
    }

    protected override IEnumerator RecoveryFrames()
    {
	    yield return base.RecoveryFrames();
	    _boss.Animator.SetTrigger(Roar);
	    yield return StartCoroutine(WaitForAnimationTrigger(RoarString));
    }

    public void ShieldDamage(int amount)
    {
	    _sustainedDamage += amount;
	    if (gameObject.activeSelf)
	    {
		    AudioManager.Play("CrabGuts");
	    }
    }
}
