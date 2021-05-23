/** ------------------------------------------------------------------------------
- Filename:   DashAttack
- Project:    MI498-SP21-IronGalaxy 
- Developers: IronGalaxy
- Created on: 2021/02/17
- Created by: shawn
------------------------------------------------------------------------------- */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** DashAttack.cs
 * --------------------- Description ------------------------
 * Crab rushes sideways at the player. The side that the crab is facing has a
 * larger hit box and the player will get swiped by its claws if they dodge to that side.
 * If the player dodges to the back of the crab, they may be able to land a hit on the
 * crab as it is rushing past.
 *
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/02/17       Created file.                                       shawn
 */
public class DashAttack : ColliderBasedAttack
{
    /* PUBLIC MEMBERS */

    [Tooltip("Dash FX turned on during dash, turned off after dash")]
    [SerializeField] private GameObject _dashFX = null;

    [Header("Wall hit variables")]
    [Tooltip("When this attack hits something besides the player, it will " +
             "stun the boss for this many seconds")]
    [Min(0f)]
    [SerializeField] private float _wallHitWaitTime = 5f;
    [SerializeField] protected float _wallHitCamShake = 200f;
    [SerializeField] protected string _stunAnimation = "stunned";
    [Tooltip("The effect to play when hitting a wall")]
    [SerializeField] protected GameObject _wallHitEffect = null;
    [Tooltip("The effect to play when hitting a wall after slipping on ice")]
    [SerializeField] protected GameObject _wallHitEffectSlipped = null;

    [Header("Ice Crash Info")] 
    [Tooltip("If the boss goes over icy paint while performing this dash and proceeds to hit a wall," +
             "the Wall Hit Wait Time is extended by this value")]
    [SerializeField]
    [Min(0)]
    protected float _crashStunTimeMod = 2f;
	[Tooltip("If the boss goes over icy paint while performing this dash and proceeds to hit a wall," +
	         "This amount of damage is inflicted upon the boss")]
    [SerializeField]
	[Min(0)]
    protected int _iceCrashDamage = 10;
    [Tooltip("Damage multiplier to apply to the boss' health when they are stunned from a crash.\n" +
             "Values above one make the boss take more damage, values below one make the boss take " +
             "less damage.")]
    [SerializeField]
    [Min(0f)]
    protected float _iceCrashDamageMult = 1.1f;
    /* PRIVATE MEMBERS */
    protected bool _collided = false;

    protected const float MaximumCollisionWaitTime = 10f;

    protected bool _hitIce = false;
    private static readonly int Slipping = Animator.StringToHash("slipping");

    /// <summary>
    /// True if any of our colliders have indicated that they last
    /// collided with the player
    /// </summary>
    protected bool LastCollisionWasWithPlayer
    {
	    get
	    {
			// check if the collision was with the player
		    foreach (var col in _colliders)
		    {
			    if (col.lastCollisionWasPlayer)
			    {
				    return true;
			    }
		    }
		    return false;
	    }
    }
    
    /* PROPERTIES */


    /* METHODS */

    protected override IEnumerator StartupFrames()
    {
	    yield return StartCoroutine(WaitForAnimationTrigger(_startAnimTrigger));
	    // update direction to player to make sense
	    _toPlayer = _player.transform.position - _boss.transform.position;
	    OverrideDirection = _toPlayer.normalized;
    }

    protected override IEnumerator ActiveFrames()
    {
	    _hitIce = false;
	    _boss.Rigidbody.velocity = _toPlayer.normalized * (_boss.Speed * _initialSpeedMult);
	    SetColliderStatus(true);
	    SetRenderEnabled(true);
	    _collided = false;
        SetDashFX(true);
	    float timeWaited = 0f;
	    Vector3 lastVelocity = _toPlayer.normalized * (_boss.Speed * _initialSpeedMult);
	    _boss.Rigidbody.velocity = lastVelocity;
	    while (!_collided && timeWaited < MaximumCollisionWaitTime && 
	           _boss.Rigidbody.velocity.magnitude >= lastVelocity.magnitude - lastVelocity.magnitude * 0.5f)
	    {
		    lastVelocity = _toPlayer.normalized * (_boss.Speed * _initialSpeedMult);
		    _boss.Rigidbody.velocity = lastVelocity;
		    timeWaited += Time.deltaTime;
		    // check for ice
		    if (_boss.IceEffect.inPaint)
		    {
			    _hitIce = true;
			    _boss.Animator.SetTrigger(Slipping);
		    }
		    yield return null;
	    }
        SetDashFX(false);
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
		    AudioManager.Play("WallCrash");
	    }
	    else if (!_hitIce)
	    {
		    AudioManager.Play("WallCrashLight");
	    }
	    _boss.SetDebugBehaviorText(gameObject.name + " cooldown");
        yield return StartCoroutine(SlowBoss(_recoveryFrames));
	    if (lcwp)
	    {
		    yield break;
	    }
	    // if it wasn't, we are stunned
	    var cs = _boss.CamShaker;
	    if (cs)
	    {
		    cs.GenerateImpulse(_wallHitCamShake);
	    }
	    _boss.Animator.SetBool(_stunAnimation, true);
        PlayWallHitSparks(_hitIce);
        int damIndex = 0;
        if (_hitIce)
        {
	        damIndex = _boss.Health.AddDamageMultiplier(_iceCrashDamageMult);
            StaticEffectCallBase.PlayEffect("CeilingDust");
        }
	    yield return new WaitForSeconds(_wallHitWaitTime);
	    if (_hitIce)
	    {
		    yield return new WaitForSeconds(_crashStunTimeMod);
		    _boss.Health.RemoveDamageMultiplier(damIndex);
	    }
	    _boss.Animator.SetBool(_stunAnimation, false);
	    yield return StartCoroutine(WaitForIdleTrigger());
    }

    public override void StopAttackBehavior()
    {
	    _boss.Animator.SetBool(_stunAnimation, false);
	    base.StopAttackBehavior();
    }

    private void PlayWallHitSparks(bool slipped = true)
    {
        if (!slipped)
        {
            if (_wallHitEffect != null)
            {
                GameObject effect = GameObject.Instantiate(_wallHitEffect, transform.position, _wallHitEffect.transform.localRotation, null);
            }
        }
        else
        {
            if (_wallHitEffectSlipped != null)
            {
                GameObject effect = GameObject.Instantiate(_wallHitEffectSlipped, transform.position, _wallHitEffectSlipped.transform.localRotation, null);
            }
        }
    }

    private void SetDashFX(bool active)
    {
        if (_dashFX != null)
        {
            _dashFX.SetActive(active);
        }
    }

    public void TriggerCollision()
    {
	    const float collisionForgivenessAngle = 90f;
	    foreach (var col in _colliders)
	    {
		    if (col.lastCollisionWasPlayer)
		    {
			    continue;
		    }
		    if (col.collisionDirection != Vector2.zero && Vector2.Angle(col.collisionDirection.normalized, 
			        OverrideDirection.normalized) <= collisionForgivenessAngle)
		    {
			    return;
		    }
	    }
	    _collided = true;
    }
}
