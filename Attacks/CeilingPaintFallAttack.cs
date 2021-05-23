/** ------------------------------------------------------------------------------
- Filename:   CeilingPaintFallAttack
- Project:    Brush Knight 
- Developers: Team BoSS
- Created on: 2021/03/08
- Created by: shawn
------------------------------------------------------------------------------- */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** CeilingPaintFallAttack.cs
 * --------------------- Description ------------------------
 * A type of attack which periodically creates paint globs around the
 * player that fall from the ceiling
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/03/08       Created file.                                       shawn
 */
public class CeilingPaintFallAttack : AbstractAttack
{
    /* PUBLIC MEMBERS */
	

    /* PRIVATE MEMBERS */
    [Space]
    [Tooltip("Prefab for the object which falls from the ceiling and creates paint")]
    [SerializeField] private CeilingGlob _ceilingGlobPrefab = null;
    [Tooltip("Range indicating the number of globs to create [minimumInclusive, maximumInclusive]")]
    [SerializeField] private Vector2Int _globCount = Vector2Int.one;
	[Tooltip("In seconds")]
    [SerializeField] private float _globSpawnRate = 2f;
	[Tooltip("Maximum distance around the player that a glob can spawn")]
	[SerializeField] private float _attackRadius = 4f;
	[Tooltip("True if the boss will not take damage during this attack")]
	[SerializeField] private bool _invincible = true;

	private Coroutine _bossShaker = null;

	private GameObject _poolParent = null;
	private List<CeilingGlob> _globPool = new List<CeilingGlob>();
	private int _poolArrayIndex = 0;
	
    /* PROPERTIES */

	
    /* METHODS */


    protected override IEnumerator StartupFrames()
    {
	    yield return StartCoroutine(WaitForAnimationTrigger(_startAnimTrigger));
	    if (_invincible)
	    {
		    _boss.Health.DamageImmune = true;
	    }
	    // start shaking
	    if (_bossShaker != null)
	    {
		    StopCoroutine(_bossShaker);
	    }
	    //_bossShaker = StartCoroutine(ShakeBoss());
    }

    protected override IEnumerator ActiveFrames()
    {
	    int globCount = Random.Range(_globCount.x, _globCount.y + 1);
	    while (globCount > 0)
	    {
		    // drop a new glob
		    var cg = _globPool[_poolArrayIndex];
		    cg.gameObject.SetActive(true);
		    Vector2 position = _player.transform.position;
		    Vector2 offset = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
		    position += offset.normalized * Random.Range(0f, _attackRadius);
		    cg.Drop(position, _attackColor, this);
		    _poolArrayIndex = (_poolArrayIndex + 1) % _globPool.Count;
		    // wait to create the next glob
		    globCount--;
		    if (globCount > 0)
		    {
			    yield return new WaitForSeconds(_globSpawnRate);
		    }
	    }
	    _boss.Health.DamageImmune = false;
    }

    protected override IEnumerator RecoveryFrames()
    {
	    // stop shaking
	    if (_bossShaker != null)
	    {
		    StopCoroutine(_bossShaker);
		    _bossShaker = null;
	    }
	    //yield return StartCoroutine(WaitForFrames(_recoveryFrames));
	    yield return StartCoroutine(WaitForAnimationTrigger(_endAnimTrigger));
    }

    private IEnumerator ShakeBoss()
    {
	    const float shakeRange = 0.2f;
	    Vector2 shakeCenter = _boss.transform.position;
	    while (isActiveAndEnabled)
	    {
		    Vector2 offset = new Vector2(
			    Random.Range(-shakeRange, shakeRange),
			    Random.Range(-shakeRange, shakeRange));
		    _boss.transform.position = shakeCenter + offset;
		    yield return new WaitForSeconds(0.05f);
	    }
	    _bossShaker = null;
    }
    
    public override void StopAttackBehavior()
    {
	    if (_boss && _boss.Health)
	    {
		    _boss.Health.DamageImmune = false;
	    }
	    base.StopAttackBehavior();
    }

    protected override void AwakeExtend()
    {
	    _poolParent = new GameObject(gameObject.name + " droplets");
	    for (int i = 0; i < _globCount.y; i++)
	    {
		    CeilingGlob cg = Instantiate(_ceilingGlobPrefab, _poolParent.transform);
		    cg.gameObject.SetActive(false);
		    _globPool.Add(cg);
	    }
    }
}
