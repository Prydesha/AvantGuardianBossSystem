/** ------------------------------------------------------------------------------
- Filename:   AbstractAttack
- Project:    MI498-SP21-IronGalaxy 
- Developers: IronGalaxy
- Created on: 2021/02/02
- Created by: shawn
------------------------------------------------------------------------------- */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

/** AbstractAttack.cs
 * --------------------- Description ------------------------
 * Stores data and behaviors for an attack which can be performed
 * by a boss 
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/02/02       Created file.                                       shawn
 * 2021/02/16		Added ability for attacks							Shawn
 *					to create paint
 * 2021/02/25		Utility factors are now connected					Shawn
 *					to boss phases
 */
public abstract class AbstractAttack : MonoBehaviour
{
    /* PUBLIC MEMBERS */
    public const int AbsoluteMaxConsecutiveAttacks = 5;

    /// <summary>
    /// Event which occurs when this attack ends its behavior
    /// </summary>
    [HideInInspector] public UnityEvent OnAttackEnd = new UnityEvent();
    
    [Tooltip("Set this to the maximum number of times this move can be performed consecutively " +
             "before its utility is automatically set to zero (if this value is set to zero, " +
             "then repetitions of this attack will be irrelevant)")]
    [Range(0, AbsoluteMaxConsecutiveAttacks)]
    public int maximumConsecutiveUses = 1;
    [Tooltip("The association between consecutive uses of this attack and its utility score")]
    public AnimationCurve repUtilFunction = AnimationCurve.Linear(0, 0, 1, 1);
    [Tooltip("The elements which affect our behavior (order does not matter, the list is sorted at runtime)")]
    [SerializeField] protected List<PhaseBasedUtilities> phaseFactors = new List<PhaseBasedUtilities>();

    [Tooltip("As long as the utility of this attack is non-zero, this amount can be randomly " +
             "added to its calculated utility")] 
    [Range(0f, 1f)]
    public float randomUtilBonusMax = 0.01f;

    // the last calculated utility score of this attack
    [HideInInspector] public float lastCalculatedUtility = 0f;

    #region Immediate Phase Transition Variables
    [Header("Action Transitioning")]
    [Tooltip("If set to an action, that action will immediately precede this attack regardless of utility")]
    public Action transitioningAction = null;
    [Tooltip("If set to an attack, that attack will immediately precede this attack regardless of " +
             "utility (overrides Transitioning Action)")]
    public AbstractAttack transitioningAttack = null;
    [Space]
    #endregion
    
    /* PRIVATE MEMBERS */
    [Tooltip("Damage this attack inflicts on the main character")]
    [Min(0f)]
    [SerializeField] private int _damage = 10;
    [Tooltip("Amount of knockback applied to the player when this attack damages them")]
    [SerializeField] private float _knockbackPower = 20f;
    [Tooltip("True if this attack moves the boss on its own")]
    [SerializeField] private bool _overrideMovement = false;
    [Tooltip("Increase this to make the boss have a faster initial 'lunge' at the player")]
    [SerializeField] protected float _initialSpeedMult = 4f;
    [Tooltip("Not used for collider based attacks")]
    [Min(0)] [SerializeField] protected int _startupFrames = 20;
    [Min(0)] [SerializeField] protected int _activeFrames = 5;
    [Min(0)] [SerializeField] protected int _recoveryFrames = 20;
	[FormerlySerializedAs("_audioQueue")]
	[Tooltip("Name of the sound effect to play when this attack enters its startup frames")]
	[SerializeField] protected string _startupAudioQueue = "";
	[Tooltip("Name of the sound effect to play when this attack enters its active frames")]
	[SerializeField] protected string _activeAudioQueue = "";
	[FormerlySerializedAs("_animationTrigger")]
	[Tooltip("At the start of this attack (leave blank to avoid a trigger call)")]
	[SerializeField] protected string _startAnimTrigger = "";
	[Tooltip("At the end of this attack (leave blank to avoid a trigger call)")]
	[SerializeField] protected string _endAnimTrigger = "";
	
	[Header("Paint")]
	[Tooltip("Percentage chance that this attack will create paint")]
	[Range(0f, 1f)]
	public float _paintProbability;
	[Tooltip("The texture that is painted on the ground whenever this attack paints")]
	[SerializeField] private Texture2D _paintTexture = null;
	[Tooltip("This should be a sibling or child of the colliders of this attack, " +
	         "it denotes where paint will be placed")]
	[SerializeField] private Transform _paintLayDownPos = null;
	[Tooltip("Range of possible scale values for the placed paint sprite")]
	[SerializeField] private Vector2 _paintScaleRange = Vector2.one;
	[Tooltip("Decides the rotation of the placed paint texture\n\n" +
	         "Fixed: rotation is always zero\n\n" + 
	         "Random: rotation can be any value from 0 to 360\n\n" + 
	         "PainterAligned: rotation matches the direction of the attack")]
	[SerializeField] private PaintLayDownOrientation _orientation = PaintLayDownOrientation.PainterAligned;
	[Tooltip("Indicates how this attack handles emitting lightning projectiles")]
	[SerializeField] private LightningEmissionType _lightningEmission = LightningEmissionType.None;
	[Tooltip("Colors of paint which this attack can create")]
	public ColorChancePair[] _paintColors;
	
	[Space]

	private Coroutine _attackBehavior = null;

	private bool _triggerBehavior = false;

    protected Boss _boss = null;
    // vector to the player when this attack was started
    protected Vector2 _toPlayer = Vector2.zero;
    protected Player _player = null;
	
    // used to trigger the entrance of active frames
    protected bool _activeFramesTrigger = false;
    
    // the color that this attack is currently placing
    protected Color _attackColor;

    // used to wait for the idle animator state
    protected bool _waitingForIdle = false;

    /// <summary>
    /// Absolute maximum time to wait for a trigger before it is re-triggered
    /// </summary>
    protected const float MAXIMUM_WAIT_TRIGGER = 5f;
    
    /* PROPERTIES */

    /// <summary>
    /// Damage this attack inflicts on the main character
    /// </summary>
    public int Damage => _damage;

    /// <summary>
    /// True if this attack is currently occurring in the world
    /// </summary>
    public bool Active => _attackBehavior != null;
	/// <summary>
	/// True if this attack moves the boss on its own
	/// </summary>
    public bool OverrideMovement => _overrideMovement;
	/// <summary>
	/// Reference to the game's current player instance
	/// </summary>
	public Player Player => _player;

	/// <summary>
	/// Direction that this attack is currently attacking in
	/// </summary>
	public Vector2 ToPlayer => _toPlayer;

	/// <summary>
	/// Amount of knockback applied to the player when this attack damages them
	/// </summary>
	public float KnockbackPower => _knockbackPower;

	/// <summary>
	/// Represents the direction of movement that this attack is
	/// currently applying to the boss
	/// </summary>
	public Vector2 OverrideDirection
	{
		get;
		protected set;
	}

    /// <summary>
    /// Gets the attack color
    /// </summary>
    public Color attackColor
    {
        get
        {
            return _attackColor;
        }
    }
	
	/// <summary>
	/// Simple encapsulator for the null color value of the PaintColorAuthoritative
	/// </summary>
	protected Color NullColor => PaintColorAuthoritative.GetColor(PaintColorAuthoritative.PaintColorEnum.Null);
	
    /* METHODS */

    /// <summary>
    /// Execute the behavior associated with this attack
    /// </summary>
    /// <param name="bossBody">rigidbody of the calling boss</param>
    /// <param name="toPlayer">the vector denoting the direction and distance
    /// between the boss and player</param>
    /// <param name="player">reference to the game's player instance</param>
    public void PerformAttack(Boss boss, Vector2 toPlayer, Player player)
    {
	    gameObject.SetActive(true);
        if (_attackBehavior != null)
        {
            StopCoroutine(_attackBehavior);
        }
        _boss = boss;
        _toPlayer = toPlayer;
        _player = player;
        OverrideDirection = _toPlayer;
        try
        {
	        TriggerAttackBehaviorCoroutine();
        }
        catch (Exception e)
        {
	        Debug.Log(e);
	        _triggerBehavior = true;   
        }
    }

    /// <summary>
    /// Tells this attack to enter its active frames
    /// </summary>
    public virtual void TriggerActiveFrames()
    {
	    _activeFramesTrigger = true;
    }

    /// <summary>
    /// Getter for the list of utility factors associated with this
    /// attack during a specific phase of combat. If no factor set is specified for the desired phase, 
    /// returns the closest set
    /// </summary>
    /// <param name="phaseNumber">number of the current phase of combat (starting from 1)</param>
    /// <returns></returns>
    public FactorSet GetFactorsForPhase(int phaseNumber)
    {
	    if (phaseFactors.Count < 1)
	    {
		    return null;
	    }
	    foreach (var pf in phaseFactors)
	    {
		    if (pf.phase == phaseNumber)
		    {
			    return pf.factors;
		    }
	    }
	    return phaseFactors[phaseFactors.Count - 1].factors;
    }

    /// <summary>
    /// Base declaration for the behavior performed by this attack.
    /// </summary>
    private IEnumerator AttackBehavior()
    {
	    _boss.SetDebugBehaviorText(gameObject.name);
	    _attackColor = GetRandomUsableColor();
	    var bAnim = _boss.Animator;
	    _activeFramesTrigger = false;
	    if (_startAnimTrigger != "" && bAnim)
	    {
		    _boss.Animator.SetTrigger(_startAnimTrigger);
	    }
	    if (_startupAudioQueue != "")
	    {
		    AudioManager.Play(_startupAudioQueue);
	    }
	    yield return StartCoroutine(StartupFrames());
	    _activeFramesTrigger = false;
	    if (_activeAudioQueue != "")
	    {
		    AudioManager.Play(_activeAudioQueue);
	    }
	    yield return StartCoroutine(ActiveFrames());
	    _activeFramesTrigger = false;
	    if (_endAnimTrigger != "" && bAnim)
	    {
		    bAnim.SetTrigger(_endAnimTrigger);
	    }
	    if (_attackColor != NullColor)
	    {
		    LayDownPaint();
	    }
		#region Handle lightning effects
	    var lightningManager = _boss.LightningEffect;
	    if (lightningManager.inPaint)
	    {
		    switch (_lightningEmission)
		    {
			    case LightningEmissionType.AllCardinals:
				    for (float f = 0f; f < 360f; f += 45f)
				    {
					    lightningManager.CreateLightningProjectile(transform.position, f.Deg2Vector());
				    }

				    break;
			    case LightningEmissionType.PlayerDir:
				    lightningManager.CreateLightningProjectile(transform.position, -_toPlayer.normalized);
				    break;
		    }
	    }
	    #endregion
	    yield return StartCoroutine(RecoveryFrames());
	    _attackBehavior = null; 
	    OnAttackEnd?.Invoke();
	    _boss.EndOfAttackBehavior(this);
    }

    /// <summary>
    /// startup frame behavior
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerator StartupFrames();

    /// <summary>
    /// active frame behavior
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerator ActiveFrames();

    /// <summary>
    /// recovery frame behavior
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerator RecoveryFrames();

    /// <summary>
    /// Called just before the start of the game
    /// </summary>
    private void Awake()
    {
        gameObject.SetActive(false);
        OnAttackEnd.AddListener(() => gameObject.SetActive(false));
        // sort phases
        phaseFactors.Sort((u1, u2) => u1.phase.CompareTo(u2.phase));
        AwakeExtend();
    }
    
    /// <summary>
    /// Overrideable method for inheriting classes to
    /// add their own on awake behavior 
    /// </summary>
    protected virtual void AwakeExtend(){}

    /// <summary>
    /// called at the end of every frame
    /// </summary>
    private void LateUpdate()
    {
	    // I wish I didn't have to add an update to this, but apparently activating 
	    // a gameobject only happens consistently before the end of a frame
	    if (_triggerBehavior)
	    {
		    TriggerAttackBehaviorCoroutine();
	    }
    }
    
    /// <summary>
    /// Inner method used to officially start performing this attack and trigger any
    /// secondary effects associated with that
    /// </summary>
    private void TriggerAttackBehaviorCoroutine()
    {
	    _triggerBehavior = false;
	    gameObject.SetActive(true);
	    _attackBehavior = StartCoroutine(AttackBehavior());
    }

    /// <summary>
    /// Immediately cease any behavior related to this attack.
    /// </summary>
    /// <remarks>Overriding behaviors should undo any changes
    /// they had for the boss in this function (also calling the base implementation)</remarks>
    public virtual void StopAttackBehavior()
    {
	    if (_attackBehavior != null)
	    {
		    StopCoroutine(_attackBehavior);
	    }
	    if (_boss && _boss.Health)
	    {
		    _boss.Health.DamageImmune = false;
	    }
	    _attackBehavior = null;
	    gameObject.SetActive(false);
	    if (_endAnimTrigger != "" && _boss && _boss.Animator)
	    {
		    _boss.Animator.SetTrigger(_endAnimTrigger);
	    }
    }

    /// <summary>
    /// Based on the probability weights of each of the possible colors
    /// of this attack, determines a random color and returns it
    /// </summary>
    /// <returns></returns>
    protected Color GetRandomUsableColor()
    {
	    Color c = NullColor;
	    float rand = Random.Range(0f, 1f);
	    float cumulativeProb = 0f;
	    foreach (var ccp in _paintColors)
	    {
		    //TODO: make sure ccp's are ordered
		    if (_boss.CurrentPhaseIndex + 1 < ccp.phase)
		    {
			    continue;
		    }
		    if (rand < ccp.probability + cumulativeProb)
		    {
			    c = ccp.color;
			    break;
		    }
		    cumulativeProb += ccp.probability;
	    }
	    return c;
    }

    /// <summary>
    /// Used to paint the world
    /// </summary>
    protected void LayDownPaint()
    {
	    if (PaintManagerBase.PM && _paintTexture && _paintLayDownPos)
	    {
		    if (Random.Range(0f, 1f) < _paintProbability)
		    {
			    float angle;
			    switch (_orientation)
			    {
				    case PaintLayDownOrientation.Fixed:
				    default:
					    angle = 0;
					    break;
				    case PaintLayDownOrientation.Random:
					    angle = Random.Range(0, 360);
					    break;
				    case PaintLayDownOrientation.PainterAligned:
					    angle = Vector2.SignedAngle(Vector2.down, _toPlayer);
					    break;
			    }
			    PaintManagerBase.PM.Draw(_paintLayDownPos.position, angle,
				    Random.Range(_paintScaleRange.x, _paintScaleRange.y), _paintTexture, _attackColor);
			    AudioManager.Play("BrushSwingHeavy");
		    }
	    }
    }

    /// <summary>
    /// Function called whenever the boss using this attack
    /// has entered its idle state
    /// </summary>
    public virtual void OnIdleEntered()
    {
	    _waitingForIdle = false;
    }
    
    /// <summary>
    /// Coroutine which simply waits for a specified number of frames
    /// (use within other coroutines)
    /// </summary>
    /// <param name="frameCount"></param>
    /// <returns></returns>
    protected IEnumerator WaitForFrames(int frameCount)
    {
	    int curFrameCount = 0;
	    while (curFrameCount < frameCount)
	    {
		    yield return new WaitForEndOfFrame();
		    curFrameCount++;
	    }
    }

    /// <summary>
    /// Waits for the boss of this attack to 
    /// enter its idle state. If that state is
    /// not achieved in a reasonable amount of time,
    /// this wait halts itself
    /// </summary>
    /// <returns></returns>
    protected IEnumerator WaitForIdleTrigger()
    {
	    _waitingForIdle = true;
	    float t = 4f;
	    while (_waitingForIdle && t > 0f)
	    {
		    t -= Time.deltaTime;
		    yield return null;
	    }
    }

    /// <summary>
    /// Waits for a triggered event from the boss animator before continuing
    /// </summary>
    /// <param name="triggerName">If the event does not occur after a set maximum amount of time
    /// this animation trigger is activated</param>
    /// <returns></returns>
    protected IEnumerator WaitForAnimationTrigger(string triggerName)
    {
	    _activeFramesTrigger = false;
	    float timeInWait = 0f;
	    bool temp = false;
	    while (!_activeFramesTrigger)
	    {
		    timeInWait += Time.deltaTime;
		    if (timeInWait > MAXIMUM_WAIT_TRIGGER)
		    {
			    if (!temp)
			    {
				    _boss.Animator.SetTrigger(triggerName);
				    timeInWait = 0f;
				    temp = true;
			    }
			    else
			    {
				    yield break;
			    }
		    }
		    yield return null;
	    }
    }
}

/// <summary>
/// Associates a color with a probability of occurrence
/// </summary>
[System.Serializable]
public struct ColorChancePair
{
    [Tooltip("The enum value of the color to use")]
	[SerializeField] private PaintColorAuthoritative.PaintColorEnum _color;
    // Accessor to the color referenced by _color
    public Color color
    {
        get
        {
            return PaintColorAuthoritative.GetColor(_color);
        }
        set
        {
            _color = PaintColorAuthoritative.GetValue(value);
        }
    }
    [Tooltip("Probablity of this color")]
	[Range(0f, 1f)]
	public float probability;
	[Tooltip("Phase where this color is applicable")]
	[Min(1)]
	public int phase;
	
	public ColorChancePair(Color c, float p, int phaseNumber)
	{
		_color = PaintColorAuthoritative.GetValue(c);
		probability = p;
		phase = phaseNumber;
	}
}

/// <summary>
/// Represents a set of utility factors for an attack to use
/// during a specific phase of a boss fight
/// </summary>
[System.Serializable]
public struct PhaseBasedUtilities
{
	[Tooltip("Integer value of the corresponding boss phase for this set of utilities (1 correlates to " +
	         "the first phase of a boss, 2 to the second, and so on)")]
	[Min(1)]
	public int phase;
	[Tooltip("Set of Utility Factors for this attack to use during the specified phase")]
	public FactorSet factors;

	public PhaseBasedUtilities(int p, FactorSet fs)
	{
		phase = p;
		factors = fs;
	}
}

/// <summary>
/// Used to determine the rotation of placed paint for an attack
/// </summary>
public enum PaintLayDownOrientation
{
	Fixed = 0,
	Random = 1,
	PainterAligned = 2
}

/// <summary>
/// Indicates how an attack uses lightning paint projectiles
/// </summary>
public enum LightningEmissionType
{
	None = 0,
	PlayerDir = 1,
	AllCardinals = 2
}
