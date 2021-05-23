/** ------------------------------------------------------------------------------
- Filename:   SingleRampageDash
- Project:    Brush Knight 
- Developers: Team BoSS
- Created on: 2021/03/03
- Created by: shawn
------------------------------------------------------------------------------- */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** SingleRampageDash.cs
 * --------------------- Description ------------------------
 *
 *
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/03/03       Created file.                                       shawn
 */
public class SingleRampageDash : DashAttack
{
    /* PUBLIC MEMBERS */


    /* PRIVATE MEMBERS */


    /* PROPERTIES */


    /* METHODS */

    protected override IEnumerator ActiveFrames()
    {
	    var tpn = _toPlayer.normalized;
	    // randomize direction general to player
	    Vector2 dashDirection;
		dashDirection.x = tpn.x > 0 ? Random.Range(0.01f, tpn.x) : Random.Range(tpn.x, -0.01f);
		dashDirection.y = tpn.y > 0 ? Random.Range(0.01f, tpn.y) : Random.Range(tpn.y, -0.01f);
	    dashDirection.Normalize();
	    OverrideDirection = dashDirection;
	    // perform dash
	    _boss.Rigidbody.velocity = dashDirection * (_boss.Speed * _initialSpeedMult);
	    SetColliderStatus(true);
	    SetRenderEnabled(true);
	    _collided = false;
	    float timeWaited = 0f;
	    while (!_collided && timeWaited < MaximumCollisionWaitTime)
	    {
		    _boss.Rigidbody.velocity = dashDirection * (_boss.Speed * _initialSpeedMult);
		    timeWaited += Time.deltaTime;
		    yield return null;
	    }
	    _collided = false;
	    SetColliderStatus(false);
	    SetRenderEnabled(false);
    }

    protected override IEnumerator RecoveryFrames()
    {
	    var lcwp = LastCollisionWasWithPlayer;
	    if (_hitIce && !lcwp)
	    {
		    _boss.Health.Damage(_iceCrashDamage);
	    }
	    var cs = _boss.CamShaker;
	    if (cs)
	    {
		    cs.GenerateImpulse(_wallHitCamShake);
	    }
	    _boss.Animator.SetBool(_stunAnimation, true);
	    _boss.SetDebugBehaviorText(gameObject.name + " cooldown");
	    yield return StartCoroutine(WaitForFrames(_recoveryFrames));
	    _boss.Animator.SetBool(_stunAnimation, false);
    }
}
