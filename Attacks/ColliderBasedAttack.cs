/** ------------------------------------------------------------------------------
- Filename:   ColliderBasedAttack
- Project:    MI498-SP21-IronGalaxy 
- Developers: IronGalaxy
- Created on: 2021/02/02
- Created by: shawn
------------------------------------------------------------------------------- */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** ColliderBasedAttack.cs
 * --------------------- Description ------------------------
 * Attack behavior for a basic attack which involves turning on a collider
 * and detecting collisions with the player
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/02/02       Created file.                                       shawn
 * 2021/02/12		Renamed from BasicSwipeAttack to					Shawn
 *					ColliderBasedAttack
 * 2021/02/16		Reworked behavior to use frames						Shawn
 *					rather than time
 * 2021/02/24		Changed startup to be based on						Shawn
 *					animation events rather than frame count
 */
public class ColliderBasedAttack : AbstractAttack
{
	[Space] 
	[Tooltip("If set to true, this attack will rotate its transform to an intermediate " +
	         "cardinal direction facing towards the player when it is selected as the current active attack")]
	[SerializeField] protected bool _facesPlayer = true;
	[SerializeField] private SpriteRenderer[] _swipeRenderers = new SpriteRenderer[0];

	// the colliders which trigger damage to another entity (our player)
    protected AttackCollider[] _colliders = new AttackCollider[0];

    /// <summary>
    /// Enables or Disables the colliders of this attack
    /// </summary>
    /// <param name="colEnabled">true to enable</param>
    protected void SetColliderStatus(bool colEnabled)
    {
	    foreach (var cc in _colliders)
	    {
		    cc.enabled = colEnabled;
	    }
    }
    
    protected void SetRenderEnabled(bool value)
    {
	    foreach (var sr in _swipeRenderers)
	    {
		    if (!sr)
		    {
			    continue;
		    }
		    sr.color = Color.white;
		    sr.gameObject.SetActive(value);
	    }
    }

    /// <summary>
    /// Behavior performed during startup frames
    /// </summary>
    /// <returns></returns>
    protected override IEnumerator StartupFrames()
    {
	    if (_facesPlayer)
	    {
		    // face the player
		    Vector2 playerPos = (Vector2) _boss.transform.position + _toPlayer;
		    transform.LookAt2D(playerPos, true);
	    }
	    SetColliderStatus(false);
	    // switch renderer colors
	    foreach (var sr in _swipeRenderers)
	    {
		    sr.gameObject.SetActive(false);
		    sr.color = Color.Lerp(_attackColor, Color.white, 0.5f);
	    }
	    _activeFramesTrigger = false;
	    _boss.Rigidbody.velocity = _toPlayer.normalized * (_boss.Speed * _initialSpeedMult);
	    yield return StartCoroutine(WaitForAnimationTrigger(_startAnimTrigger));
    }
    
    /// <summary>
    /// Behavior performed during active frames
    /// </summary>
    /// <returns></returns>
    protected override IEnumerator ActiveFrames()
    {
	    SetColliderStatus(true);
	    SetRenderEnabled(true);
	    yield return StartCoroutine(WaitForFrames(_activeFrames));
	    SetColliderStatus(false);
	    SetRenderEnabled(false);
    }
    
    /// <summary>
    /// Behavior performed during recovery frames
    /// </summary>
    /// <returns></returns>
    protected override IEnumerator RecoveryFrames()
    {
	    yield return StartCoroutine(SlowBoss(_recoveryFrames));
    }

    /// <summary>
    /// Coroutine which performs a pseudo-physics based slow down
    /// </summary>
    /// <param name="frameDuration">number of frames to slow over</param>
    /// <returns></returns>
    protected IEnumerator SlowBoss(int frameDuration)
    {
	    var bossBody = _boss.Rigidbody;
	    Vector3 initVelocity = bossBody.velocity;
	    int frameCount = 0;
	    while (frameCount < frameDuration)
	    {
		    bossBody.velocity = Vector3.Lerp(initVelocity, Vector3.zero,
			    Mathf.SmoothStep(0f, 1f, (float)frameCount / frameDuration));
		    yield return new WaitForEndOfFrame();
		    frameCount++;
	    }
    }

    protected override void AwakeExtend()
    {
	    // connect to all colliders
	    _colliders = transform.GetComponentsInChildren<AttackCollider>();
	    foreach (var cc in _colliders)
	    {
		    cc.SetAttack(this);
	    }
	    SetColliderStatus(false);
    }
}
