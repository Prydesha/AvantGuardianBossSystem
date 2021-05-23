/** ------------------------------------------------------------------------------
- Filename:   BossAnimator
- Project:    Brush Knight 
- Developers: Team BoSS
- Created on: 2021/02/24
- Created by: shawn
------------------------------------------------------------------------------- */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** BossAnimator.cs
 * --------------------- Description ------------------------
 * all of our attack animations currently have a specific frame of the animation at
 * which point the active frames of the attack should begin. To signify this, they
 * have an animation event attached to the clip which triggers the moment that an
 * attack should transition into it's active phase. However, our boss' animator controller is
 * set up on a parent of the Boss script, and animation events can only trigger methods on the
 * same gameobject as their animation controller. Initially, I thought to just move the
 * boss script onto the parent gameobject, but this caused issues with the boss' movement
 * and player detection. We could refactor the boss script to work on a different gameobject
 * from the boss' rigidbody, but it was much more work to do that than to just add a new wrapper class.
 * Now we have this class to fulfill that purpose
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/02/24       Created file.                                       shawn
 */
[RequireComponent(typeof(Animator))]
public class BossAnimator : MonoBehaviour
{
    /* PUBLIC MEMBERS */
    public Boss Boss = null;

    [SerializeField] private string _roarSFX = "CrabRoar";
    [SerializeField] private float _roarShakeAmount = 10f;

    [Tooltip("Particle System to play when starting a roar")]
    [SerializeField] private ParticleSystem _roarVFXParticles = null;
    
    /* PRIVATE MEMBERS */

    private Coroutine _playerPauser = null;

    /* PROPERTIES */

    // The "Properties" section is for getters/setters of 
    // your private variables (feel free to delete this comment)

    /* METHODS */

    /// <summary>
    /// Used by animation events to tell the boss
    /// that an event has occurred
    /// </summary>
    public void TriggerAnimationEvent()
    {
	    if (Boss)
	    {
		    Boss.TriggerAnimationEvent();
	    }
    }

    /// <summary>
    /// If only animators were more versatile, then I wouldn't have to do
    /// something as stupid as this
    /// </summary>
    public void TriggerRoar()
    {
	    AudioManager.Play(_roarSFX);
	    if (_roarVFXParticles != null)
        {
            _roarVFXParticles.Stop();
            _roarVFXParticles.Play();
        }

	    if (_playerPauser != null)
	    {
		    StopCoroutine(_playerPauser);
	    }
	    _playerPauser = StartCoroutine(RoarCutsceneEnforcer());
	    var cs = Boss.CamShaker;
	    if (cs)
	    {
		    cs.GenerateImpulse(_roarShakeAmount);
	    }
    }

    public IEnumerator RoarCutsceneEnforcer()
    {
        if (GameManager.GM != null)
        {
            GameManager.GM.inCutscene = true;
            for (float t = 0; t < 1; t+=Time.deltaTime)
            {
                yield return null;
            }
            while (Boss.inPhaseEntry)
            {
                yield return null;
            }
            GameManager.GM.inCutscene = false;
        }
        _playerPauser = null;
    }

    #region Unity Functions

    // Start is called before the first frame update
    void Start()
    {
	    if (!Boss)
	    {
		    Boss = GetComponentInChildren<Boss>();
	    }   
    }
    #endregion
}
