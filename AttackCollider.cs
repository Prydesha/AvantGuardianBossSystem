/** ------------------------------------------------------------------------------
- Filename:   AttackCollider
- Project:    MI498-SP21-IronGalaxy 
- Developers: IronGalaxy
- Created on: 2021/02/02
- Created by: shawn
------------------------------------------------------------------------------- */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/** AttackCollider.cs
 * --------------------- Description ------------------------
 * Component to attach to a collider which is part of a boss' attack
 *
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/02/02       Created file.                                       shawn
 */
[RequireComponent(typeof(Collider2D))]
public class AttackCollider : MonoBehaviour
{
    /* PUBLIC MEMBERS */
    [Tooltip("event which is triggered anytime this collider detects a collision (not exclusive from dealing damage)")]
    public UnityEvent OnCollision;
    [Tooltip("used to distinguish what gameobjects are part of the player")]
    [SerializeField] private int _playerLayer = 7;
    [Tooltip("layers that are not part of this mask will not be considered for collisions")]
    [SerializeField] private LayerMask _attackingMask;
	
    // true if the last collider that hit this collider was part of the player
    [HideInInspector] public bool lastCollisionWasPlayer = false;
	// direction of the most recently accounted for collision (non-normalized)
    [HideInInspector] public Vector2 collisionDirection = Vector2.zero;

    /* PRIVATE MEMBERS */
    // reference to this collider's attack
    private AbstractAttack _attack = null;
    // current collision detector
    private Coroutine _detector = null;
    // list of colliders which were encountered during the last active frame
    private List<Collider2D> _hits = new List<Collider2D>();
    // used to find specific collisions
    private ContactFilter2D _filter;
    // the collider on this gameobject
    private Collider2D _collider;
    // list of parent gameobject to this collider. these will not be considered for collisions
    private List<Transform> _allMyParents = new List<Transform>();
    /* PROPERTIES */

    // The "Properties" section is for getters/setters of 
    // your private variables (feel free to delete this comment)

    /* METHODS */

    /// <summary>
    /// setter for the attack of this collider
    /// </summary>
    /// <param name="at"></param>
    public void SetAttack(AbstractAttack at)
    {
        _attack = at;
    }

    /// <summary>
    /// called just before the start of our game
    /// </summary>
    private void Awake()
    {
	    _filter = new ContactFilter2D();
	    _filter.layerMask = _attackingMask;
	    _filter.useTriggers = true;

	    _collider = GetComponent<Collider2D>();
	    
	    // find all parents
	    var curTrans = transform;
	    var nextParent = curTrans.parent;
	    while (nextParent != null)
	    {
		    _allMyParents.Add(nextParent);
		    curTrans = nextParent;
		    nextParent = curTrans.parent;
	    }
    }

    /// <summary>
    /// called whenever this collider becomes active
    /// </summary>
    private void OnEnable()
    {
	    if (_detector != null)
	    {
		    StopCoroutine(_detector);
	    }
	    _detector = StartCoroutine(CollisionDetection());
    }

    /// <summary>
    /// Coroutine for continuously checking collisions
    /// while this behavior is active
    /// </summary>
    /// <returns></returns>
    private IEnumerator CollisionDetection()
    {
	    List<Collider2D> incomingHits = new List<Collider2D>();
		_hits = new List<Collider2D>();
		_filter.layerMask = _attackingMask;
	    while (isActiveAndEnabled)
	    {
		    if (Physics2D.OverlapCollider(_collider, _filter, incomingHits) >= 0)
		    {
			    foreach (var hit in incomingHits.ToArray())
			    {
				    // check for new collision
				    if (!_hits.Contains(hit) && 
				        !_allMyParents.Contains(hit.transform) &&
				        hit.gameObject.activeSelf &&
				        hit.enabled && _attackingMask.ContainsLayer(hit.gameObject.layer))
				    {
						ApplyDamage(hit.gameObject);
				    }
				    else
				    {	// not a valid collision, don't consider it
					    incomingHits.Remove(hit);
				    }
			    }
			    yield return null;
			    _hits = incomingHits.ToList();
		    }
		    yield return null;
	    }
    }

    /// <summary>
    /// called whenever this collider becomes not active
    /// </summary>
    private void OnDisable()
    {
	    if (_detector != null)
	    {
		    StopCoroutine(_detector);
	    }
    }

    /// <summary>
    /// Called whenever a true collision is detected. it then
    /// determines if damage is applicable to the collision object
    /// </summary>
    /// <param name="other"></param>
    private void ApplyDamage(GameObject other) 
    {
	    var h = other.gameObject.GetComponent<Health>();
		// ignore collisions with a dashing player
		bool withPlayer = other.layer == _playerLayer;
		if (withPlayer && _attack && _attack.Player.Health.DamageImmune)
		{
			return;
		}
		//
		// trigger the collision
		//
	    OnCollision?.Invoke();
	    collisionDirection = transform.position - other.transform.position;
	    if (!_attack)
        {
            return;
        }
	    var dmg = _attack.Damage;
	    if (withPlayer)
	    {
		    h = _attack.Player.Health;
		    lastCollisionWasPlayer = true;
	    }
	    else
	    {
		    lastCollisionWasPlayer = false;
	    }
	    if (h)
	    {
		    h.Damage(dmg);
		    h.Knockback(_attack.ToPlayer, _attack.KnockbackPower);
	    }
    }
}
