/** ------------------------------------------------------------------------------
- Filename:   Shield
- Project:    Brush Knight 
- Developers: Team BoSS
- Created on: 2021/03/02
- Created by: shawn
------------------------------------------------------------------------------- */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Shield.cs
 * --------------------- Description ------------------------
 * "Attack" which disables this boss' health script until it is hit or
 * enough time has passed
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/03/02       Created file.                                       shawn
 */
public class Shield : AbstractAttack
{
    /* PUBLIC MEMBERS */
    [Space] 
    [Tooltip("At this frame during our shield's attack frames (shield frames), it will attempt to lay down paint" +
             "if this lies outside of the attack frames window, paint will be laid down after the attack frames are over")]
    [Min(0)]
    [SerializeField] private int _paintFrames = 0;

    /* PRIVATE MEMBERS */
    private bool _bossWasHit = false;

    /* PROPERTIES */

    
    /* METHODS */


    protected override IEnumerator StartupFrames()
    {
	    yield return StartCoroutine(WaitForAnimationTrigger(_startAnimTrigger));
    }

    protected override IEnumerator ActiveFrames()
    {
	    _bossWasHit = false;
	    _boss.Health.DamageImmune = true;
	    int frameCount = 0;
	    while (frameCount < _activeFrames && !_bossWasHit)
	    {
		    if (_attackColor != NullColor && frameCount == _paintFrames)
		    {
			    LayDownPaint();
			    _attackColor = NullColor;
		    }
		    frameCount++;
		    yield return new WaitForEndOfFrame();
	    }
	    _boss.Health.DamageImmune = false;
    }

    protected override IEnumerator RecoveryFrames()
    {
	    yield return StartCoroutine(WaitForAnimationTrigger(_endAnimTrigger));
    }

    public override void StopAttackBehavior()
    {
	    _boss.Health.DamageImmune = false;
	    base.StopAttackBehavior();
    }

    public void TriggerBossHit()
    {
	    _bossWasHit = true;
    }
}
