/** ------------------------------------------------------------------------------
- Filename:   CeilingGlob
- Project:    Brush Knight 
- Developers: Team BoSS
- Created on: 2021/03/08
- Created by: shawn
------------------------------------------------------------------------------- */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/** CeilingGlob.cs
 * --------------------- Description ------------------------
 * A glob of paint which falls from the ceiling and lands on the floor
 *
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/03/08       Created file.                                       shawn
 */
public class CeilingGlob : MonoBehaviour
{
    /* PUBLIC MEMBERS */
    [SerializeField] private SpriteRenderer _glob = null;
    [SerializeField] private GameObject _shadow = null;
    [Tooltip("How long in seconds it takes this droplet to fall after being created")]
    [SerializeField] private float _dropTime = 1.5f;
    [Tooltip("size of shadow when glob is at max height")]
    [SerializeField] private Vector3 _shadowStartScale = Vector3.zero;
    [Tooltip("size of shadow when glob hits the ground")]
    [SerializeField] private Vector3 _shadowEndScale = Vector3.one;
    [Tooltip("Uniform scale value for the size of the paint texture which this glob places")]
    [SerializeField] private float _paintScale = 5f;
    [SerializeField] private Texture2D _paintTexture = null;
    [Tooltip("Distance off of the ground at which this paint glob starts falling")]
    [SerializeField] private float _globStartHeight = 10f;
    [SerializeField] private AttackCollider _collider = null;

    /* PRIVATE MEMBERS */

    private Coroutine _dropper = null;
    private AbstractAttack _attack = null;

    /* PROPERTIES */

    // The "Properties" section is for getters/setters of 
    // your private variables (feel free to delete this comment)

    /* METHODS */

	/// <summary>
	/// Drop this paint glob onto the world
	/// </summary>
	/// <param name="worldPos"> where the paint glob will land</param>
	/// <param name="color"> color of the glob</param>
    public void Drop(Vector2 worldPos, Color color, AbstractAttack attack)
	{
		_attack = attack;
	    if (_dropper == null)
	    {
		    _dropper = StartCoroutine(DropPaint(worldPos, color));
	    }
    }
    private IEnumerator DropPaint(Vector2 worldPos, Color color)
    {
	    AudioManager.Play("CeilingFall");
	    _collider.gameObject.SetActive(false);
	    gameObject.SetActive(true);
	    _glob.color = color;
	    transform.position = worldPos;
	    Transform globTrans = _glob.transform;
	    Transform shadowTrans = _shadow.transform;
	    float curTime = 0f;
	    while (curTime <= _dropTime)
	    {
		    var tVal = curTime / _dropTime;
		    globTrans.localPosition = new Vector3(0, Mathf.Lerp(_globStartHeight, 0f, tVal), 0);
		    shadowTrans.localScale = Vector3.Lerp(_shadowStartScale, _shadowEndScale, tVal);
		    curTime += Time.deltaTime;
		    yield return null;
	    }
	    _dropper = null;
	    _collider.SetAttack(_attack);
	    _collider.gameObject.SetActive(true);
	    PaintManagerBase.PM.Draw(transform.position, 0f,
		    _paintScale, _paintTexture, color);
	    AudioManager.Play("GlobLand");
	    yield return null;
	    _collider.gameObject.SetActive(false);
	    gameObject.SetActive(false);
    }

    private void Awake()
    {
	    _collider.gameObject.SetActive(false);
    }
}
