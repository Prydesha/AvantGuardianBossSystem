/** ------------------------------------------------------------------------------
- Filename:   Boss
- Project:    MI498-SP21-IronGalaxy 
- Developers: IronGalaxy
- Created on: 2021/02/01
- Created by: shawn
------------------------------------------------------------------------------- */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cinemachine;
using JetBrains.Annotations;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/** Boss.cs
 * --------------------- Description ------------------------
 * The main class representing a generic boss enemy/monster
 * in our game
 * ------------------------- Log ----------------------------
 * 
 * Date             Work Description                                    Name
 * -----------      ---------------------------                         --------
 * 2021/02/01       Created file.                                       shawn
 * 2021/02/16		Incorporated paint effects with boss				Shawn
 * 2021/03/22		Ice and lightning effects. Modified					Shawn
 *					animation handling
 */
public class Boss : MonoBehaviour
{
    /* PUBLIC MEMBERS */

    /* PRIVATE MEMBERS */
    //
    // inspector shown
    //
    [Header("Core Variables")] 
    [Tooltip("If set to false, the boss will not do anything until told to do so")]
    [SerializeField] private bool _beginActive = true;
    [Tooltip("Default speed of this boss when unaffected by current states")]
    [Min(0.01f)]
    [SerializeField] private float _stdSpeed = 20f;
    [Tooltip("Default acceleration of this boss when unaffected by current states")]
    [Min(0.01f)]
    [SerializeField] private float _stdAcceleration = 5f;
    [Tooltip("After the AI has switched between two distinct actions, it must wait at least this " +
             "amount of time before it can attempt to switch to a different action")]
    [Min(0f)]
    [SerializeField] private float _minActionSwitchT = 5f;
    [Tooltip("Denoted by a red circle around the boss in gizmos")]
    [Min(0.01f)]
    [SerializeField] private float _maximumPlayerDistance = 100f;
	[Tooltip("If the player is this distance away from the boss or less, the boss is " +
	         "considered to be 'close' to the player (denoted by a green circle around the boss in gizmos)")]
    [Min(0.01f)] 
	[SerializeField] private float _playerCloseDistance = 5f;

	
	[Header("Paint Effect Variables")]
	[Tooltip("While in ice paint, standard speed's value is multiplied with this")]
	[SerializeField] private float _iceSpeedMod = 2f;
	[Tooltip("While in ice paint, standard acceleration's value is multiplied with this")]
	[SerializeField] private float _iceAccelerationMod = 0.5f;
	[SerializeField] private IcePaintEffect _iceEffect = null;
	[SerializeField] private LightningPaintEffect _lightningEffect = null;

	
	[Header("External References")]
	[SerializeField] private Health _health = null;
    
    [SerializeField] private Rigidbody2D _rigidbody = null;
    [SerializeField] private Animator _animator = null;
    [SerializeField] private BossPaintColorManager _colorManager = null;
    [SerializeField] private Text _debugBehaviorText = null;
    [SerializeField] private bool _debugBehavior = true;
    [Tooltip("List of phases which this boss can go through and " +
             "the associated actions of each phase (order does not matter, the " +
             "list is sorted at runtime)")]
    [SerializeField] protected List<PhaseEntrancePair> _phases = new List<PhaseEntrancePair>();
    [Tooltip("Event called anytime the boss enters a phase that is not its first. The passed integer value " +
             "represents the entered phase")]
    [HideInInspector] public IntUnityEvent OnPhaseChange;
    [Tooltip("List of attack behaviors which this boss can perform (this should include all attacks which the " +
             "boss can perform in any phase)")] 
    [SerializeField] protected List<AbstractAttack> _attacks = new List<AbstractAttack>();

    [Header("Extras")] 
    [SerializeField] private string _music = "CowardlyCrab";
    [Tooltip("Please talk to Shawn before changing anything with this variable")]
    [SerializeField] private List<AbstractAttack> _preOccupyAttackBuffer;
    
    //
    // true private
    //

    // Reference to the player
    // TODO: deserialize field
    [SerializeField] private Player _player = null;
    // the vector from our transform to the player's transform (recalculated once per frame)
    private Vector2 _toPlayer = Vector2.up;
    // the current action being performed by the player
    private Action _currentAction = null;
    // time until we can switch actions
    private float _timeInCurrentAction = 0f;

    #region attack tracking vars
    // the last attack which was determined to be the best to perform
    private AbstractAttack _lastCalculatedBestAttack = null;
    // the number of times LastAttack has been used consecutively
    // private int _lastAttackNumUses = 0;

    // circular array of attacks
    private AbstractAttack[] _attackBuffer = new AbstractAttack[AbstractAttack.AbsoluteMaxConsecutiveAttacks];
    // the index of the most recently added attack in the attack buffer
    private int _attackBufferIndex = -1;

    private Action _attackAction = null;
	
    // tracks the number of attacks performed during the last AttackRateInterval seconds
    private int _attacksPerformedDuringAri = 0;
    private AbstractAttack _queuedAttack = null;
    #endregion
    
    /// <summary>
    /// In game time spent around player in the last PlayerCloseTime seconds 
    /// </summary>
    private float _timeCloseToPlayer = 0f;
	// the amount of damage taken during the last DamageRateInterval time
    private float _totalDamageTakenDuringLatestInterval = 0f;
    
    // "Move Specific Start Position" denoting where an action which 
    // moves to a specific position around the boss started it's behavior
    private Vector3 _msStartPos = Vector3.zero;

    private Coroutine _utilityUpdater = null;
	// used to determine when a phase has changed
    private int _lastCalculatedPhaseIndex = 0;
	// used to indicate when a phase has changed
    private bool _phaseEntryBehaviorIsActive = false;

    // Set to false during states which cannot be left until the animator
    // has synchronized
    private bool _waitingForAnimatorIdle = false;

    private bool _hasStartedMusic = false;
    
    #region Pathfinding
    private List<PathNode> _latestPath;
    private int _pathIndex = 1;
    private float _pathfindingTimer = 0f;
    #endregion

    // bool which determines if this boss can perform actions or not
    private bool _isActive = true;
    private bool _hasActivatedOnce = false;

    private CinemachineImpulseSource _camShaker = null;
	
    // 
    // Constants
    //
    [Tooltip("The time interval that utility-factors for being close to the player should account for")]
    [Min(1f)]
    private const float PlayerCloseTime = 20f;
    [Tooltip("When determining the rate of damage received (Damage Received Per Interval (DRPI)), " +
             "this is the interval for which the boss measures that")]
    [Min(1f)]
    private const float DamageRateInterval = 4f;
    [Tooltip("When determining the boss's attack rate, " +
             "this is the interval for which the boss measures that")]
    [Min(1f)]
    private const float AttackRateInterval = 6f;
    [Tooltip("Duration in seconds of the quickest attack performable by the boss")]
    [Min(1f)]
    private const float AbsoluteMinimumAttackTime = 0.5f;
    
    /// <summary>
    /// how frequently the boss can update its movement path
    /// </summary>
    private const float PathUpdateRate = 1f;

    /// <summary>
    /// How close the boss can be to a path node before proceeding to the next node
    /// </summary>
    private const float PathNodeCloseRadius = 0.5f;
    
    /// <summary>
    /// If a utility factor's calculated value is below this value,
    /// the entire action's (or attack's) utility will be dropped to zero
    /// </summary>
    private const float UtilityFactorDropThreshold = 0.0001f;
    
    /// <summary>
    /// In seconds
    /// </summary>
    private const float UtilityUpdateRate = 0.05f;
    
    // animation constants
    private static readonly int ANIM_DIR_X = Animator.StringToHash("dir_x");
    private static readonly int ANIM_DIR_Y = Animator.StringToHash("dir_y");
    private static readonly int ANIM_WALK = Animator.StringToHash("walking");
    private static readonly string ANIM_IDLE = "Idle";

    /* PROPERTIES */

    /// <summary>
    /// True if this boss can perform behaviors and calculate utilities
    /// </summary>
    public bool Active
    {
	    get => _isActive;
	    set
	    {
		    _isActive = value;
		    if (_lastCalculatedBestAttack)
		    {
			    _lastCalculatedBestAttack.StopAttackBehavior();
		    }

		    if (_utilityUpdater != null)
		    {
			    StopCoroutine(_utilityUpdater);
		    }
		    _utilityUpdater = null;
		    if (_isActive)
		    {
			    if (!_hasActivatedOnce && _phases.Count > 0)
			    {
				    PerformPhaseEntryBehavior(_phases[0]);
				    CameraManager.Instance.CallEvent("FightIntro", transform);
			    }
			    _utilityUpdater = StartCoroutine(UtilityUpdater());
		    }

		    if (_isActive)
		    {
			    _hasActivatedOnce = true;
		    }
	    }
    }
    
    /// <summary>
    /// Current applicable acceleration for this boss' speed
    /// </summary>
    public float Acceleration
    {
        get
        {
	        var modifiedAcc = _stdAcceleration;
	        if (_iceEffect && _iceEffect.inPaint)
	        {
		        modifiedAcc *= _iceAccelerationMod;
	        }
	        return modifiedAcc;
        }
    }
    
    /// <summary>
    /// Current applicable speed for this boss
    /// </summary>
    public float Speed
    {
        get
        {
	        var modifiedSpeed = _stdSpeed;
	        if (_iceEffect && _iceEffect.inPaint)
	        {
		        modifiedSpeed *= _iceSpeedMod;
	        }
            return modifiedSpeed;
        }
    }
    
    /// <summary>
    /// True if any of this boss's attacks
    /// are currently being performed
    /// </summary>
    protected bool PerformingAttack
    {
        get
        {
	        if (_lastCalculatedBestAttack && _lastCalculatedBestAttack.Active)
	        {
		        return true;
	        }
            foreach (var at in _attacks)
            {
                if (at.Active)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// The health component of this boss
    /// </summary>
    public Health Health => _health;

    /// <summary>
    /// distance between boss and monster 
    /// </summary>
    public float PlayerDistance => _toPlayer.magnitude;
	/// <summary>
	/// In game time spent around player in the last PlayerCloseTime seconds 
	/// </summary>
    public float TimeCloseToPlayer => _timeCloseToPlayer;

	/// <summary>
	/// Reference to the paint effect which controls lightning on this boss
	/// </summary>
	public LightningPaintEffect LightningEffect => _lightningEffect;
	/// <summary>
	/// Reference to the paint effect which controls ice on this boss
	/// </summary>
	public IcePaintEffect IceEffect => _iceEffect;

	/// <summary>
    /// Encapsulator for the action currently being performed by this boss
    /// </summary>
    public Action CurrentAction
    {
        get => _currentAction;
        private set
        {
            _currentAction = value;
            _timeInCurrentAction = 0f;
            SetDebugBehaviorText(_currentAction.behavior.ToString());
            //
            // check to perform state entry behavior
            //
            switch (_currentAction.behavior)
            {
                case BossBehavior.Attack:
                    Attack();
                    break;
                case BossBehavior.MoveSpecific:
	                _msStartPos = transform.position;
	                break;
                case BossBehavior.MoveToPlayer:
	                ResetPathFind();
	                break;
            }
        }
    }

    public AbstractAttack CurrentAttack => _lastCalculatedBestAttack;
    
    /// <summary>
    /// The absolute maximum number of attacks that can be performed in the
    /// AttackRateInterval number of seconds 
    /// </summary>
    private int MaxAttacksInARI => (int)(AttackRateInterval / AbsoluteMinimumAttackTime);

    /// <summary>
    /// Getter for the current phase information of this boss, based on it's current health
    /// </summary>
    public BossPhase CurrentPhaseData
    {
	    get
	    {
		    if (_phases.Count < 1)
		    {
			    return null;
		    }
		    int cp = CurrentPhaseIndex;
		    if (cp < 0)
		    {
			    return _phases[0].Phase;
		    }
		    if (cp >= _phases.Count)
		    {
			    return _phases[_phases.Count - 1].Phase;
		    }
		    return _phases[CurrentPhaseIndex].Phase;
	    }
    }

    /// <summary>
    /// The index value of the current phase of this boss (starting from zero)
    /// </summary>
    public int CurrentPhaseIndex
    {
	    get
	    {
		    var newIndex = -1;
		    
		    if (_phases.Count > 0)
		    {
			    float healthPercentage = _health.healthPercentage;
			    // phases are ordered by their health lower bound
			    // so the first phase who's lower bound is below our current health
			    // is our current phase
			    int i;
			    for (i = 0; i < _phases.Count; i++)
			    {
				    if (_phases[i].Phase.healthLowerBound < healthPercentage)
				    {
					    break;
				    }
			    }
			    if (i == _phases.Count)
			    {
				    i--;
			    }
			    newIndex = i;
		    }
		    return newIndex;
	    }
    }

    /// <summary>
    /// Accessor for whether the boss is in it's phase entry behavior
    /// </summary>
    public bool inPhaseEntry
    {
        get { return _phaseEntryBehaviorIsActive; }
    }
    
    /// <summary>
    /// The actions performable by this boss in its current phase
    /// </summary>
    public List<Action> Actions => CurrentPhaseData.actions;
    /// <summary>
    /// Array of attack behaviors which this boss can perform
    /// </summary>
    public List<AbstractAttack> Attacks => _attacks;
	/// <summary>
	/// Reference to the boss's rigidbody
	/// </summary>
    public Rigidbody2D Rigidbody => _rigidbody;

	/// <summary>
	/// Reference to the boss' animator 
	/// </summary>
	public Animator Animator => _animator;

	/// <summary>
	/// Component which generates screen shake
	/// </summary>
	public CinemachineImpulseSource CamShaker
	{
		get
		{
			if (_camShaker == null) 
			{
				_camShaker = GetComponent<CinemachineImpulseSource>();
			}
			return _camShaker;
		}
	}

	/// <summary>
    /// This is equal to the total amount of damage received in the last
    /// DamageRateInterval divided by the length of time of the DamageRateInterval
    /// </summary>
    public float DamageReceivedPerInterval => _totalDamageTakenDuringLatestInterval / DamageRateInterval;
	/// <summary>
	/// This is equal to the player's strongest attack damage amount
	/// multiplied by the boss DamageRateInterval
	/// </summary>
    private float MaximumDamageReceivedPerInterval
    {
	    get
	    {
		    const float playerDamage = 5f;
		    return playerDamage * DamageRateInterval;
	    }
    }

    /// <summary>
    /// The color the boss is using for an attack currently
    /// </summary>
    public Color currentAttackColor
    {
        get
        {
            if (CurrentAttack != null)
            {
                return CurrentAttack.attackColor;
            }
            return PaintColorAuthoritative.GetColor(PaintColorAuthoritative.PaintColorEnum.Null);
        }
    }

    /* METHODS */

    #region Utility Methods

    /// <summary>
    /// Checks to start a new behavior at a fixed rate
    /// </summary>
    /// <returns></returns>
    private IEnumerator UtilityUpdater()
    {
	    while (gameObject.activeSelf)
	    {
		    // determine if we should/can switch actions
		    if (_timeInCurrentAction >= _minActionSwitchT && 
		        (!_currentAction || _timeInCurrentAction >= _currentAction.minimumPerformTime) && 
		        !PerformingAttack && !_waitingForAnimatorIdle)
		    {
			    if (!_hasStartedMusic)
			    {
				    AudioManager.Play(_music);
				    _hasStartedMusic = true;
			    }
			    if (_queuedAttack)
			    {
				    Attack(_queuedAttack);
				    _queuedAttack = null;
			    }
			    else if (!_phaseEntryBehaviorIsActive)
			    {
				    var bestAction = DetermineBestAction();
				    if (bestAction && (!_currentAction || _currentAction.behavior.CanTransitionTo(bestAction.behavior)))
				    {
					    CurrentAction = bestAction; //< switch behaviors
				    }
			    }
		    }
		    yield return new WaitForSeconds(UtilityUpdateRate);
		    // check for a desync between behavior and animations 
		    if (_animator.GetCurrentAnimatorStateInfo(0).IsName(ANIM_IDLE))
		    {
			    _waitingForAnimatorIdle = false;
			    if (PerformingAttack)
			    {
				    //_lastCalculatedBestAttack.StopAttackBehavior();
			    }
		    }
	    }
    }
    
    /// <summary>
    /// Iterates through each action, recalculating their utility scores
    /// and returning the best current action
    /// </summary>
    /// <returns></returns>
    private Action DetermineBestAction()
    {
        Action bestAction = null;
        float bestUtility = -1f;
        foreach (var action in CurrentPhaseData.actions)
        {
            var u = UtilityOfAction(action);
            if (u > bestUtility)
            {
                bestAction = action;
                bestUtility = u;
            }
        }
        // final check for actions which are set to this boss but not assigned in inspector
        if (_currentAction && UtilityOfAction(_currentAction) > bestUtility)
        {
	        bestAction = _currentAction;
        }
        return bestAction;
    }

    /// <summary>
    /// Calculate and return the utility score of an action.
    /// This represents how useful the action is to the boss currently
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    private float UtilityOfAction(Action action)
    {
	    action.lastCalculatedUtility = 0f; //< for debugging
        if (action.behavior == BossBehavior.Attack)
        {
            // edge case, attack action is determined by best AbstractAttack
            var bAt = BestAttack();
            int currentPhaseNumber = CurrentPhaseIndex + 1;
            if (bAt)
            {
                return UtilityOfAttack(bAt, currentPhaseNumber);
            }
        }

        var fs = action.factors;
        if (!fs || fs.TotalFactors < 1)
        {
	        return 0f;
        }
        float numerator = CombinedFactorSetUtility(fs);
        var u = numerator / fs.TotalFactors;
        if (u > 0)
        {
	        u += Random.Range(0f, action.randomUtilBonusMax);
        }
        u = Mathf.Clamp(u, action.minimumUtility, action.maximumUtility);
        action.lastCalculatedUtility = u; //< for debugging
        return u;
    }

    /// <summary>
    /// returns the sum of all utility factors of a factor set
    /// </summary>
    /// <param name="fs"></param>
    /// <returns></returns>
    public float CombinedFactorSetUtility(FactorSet fs)
    {
	    float sum = 0f;
	    // variable utilities
	    foreach (var uf in fs.VariableFactors)
	    {
		    if (uf == null)
		    {
			    continue;
		    }
		    var uOfF = UtilityOfVariableFactor(uf);
		    if (uOfF <= UtilityFactorDropThreshold)
		    {
			    return 0f;
		    }
		    sum += uOfF;
	    }
	    // color utilities
	    foreach (var cf in fs.ColorFactors)
	    {
		    if (cf == null)
		    {
			    continue;
		    }
		    var uOfF = UtilityOfColorFactor(cf);
		    if (uOfF <= UtilityFactorDropThreshold)
		    {
			    return 0f;
		    }
		    sum += uOfF;
	    }
	    return sum;
    }
    
    /// <summary>
    /// Calculate and return the utility score of an individual factor of an action.
    /// </summary>
    /// <param name="uf"></param>
    /// <returns></returns>
    public float UtilityOfVariableFactor(VariableFactor uf)
    {
        float clampedValue;
        switch (uf.type)
        {   // determine our 0 - 1 clamped value based on factor type
            case FactorType.MyHealth:
                clampedValue = _health.healthPercentage;
                break;
            case FactorType.PlayerDistance:
                clampedValue = PlayerDistance / _maximumPlayerDistance;
                break;
            case FactorType.TimeCloseToPlayer:
	            clampedValue = _timeCloseToPlayer / PlayerCloseTime;
	            break;
            case FactorType.DamageReceivedRate:
	            clampedValue = DamageReceivedPerInterval / MaximumDamageReceivedPerInterval;
	            break;
            case FactorType.PlayerDirection:
	            float angle = Vector2.SignedAngle(new Vector2(1, 0), _toPlayer.normalized);
	            if (angle < 0)
	            {
		            clampedValue = ((180f + angle) / 180f) * 0.5f;
	            }
	            else
	            {
		            clampedValue = (angle / 180f) * 0.5f + 0.5f;
	            }
	            break;
            case FactorType.TimeTaken:
	            clampedValue = _timeInCurrentAction / uf.maxValue;
	            break;
            case FactorType.AttackRate:
	            clampedValue = (float)_attacksPerformedDuringAri / MaxAttacksInARI;
	            break;
            default:
                return 0f;
        }
        clampedValue = Mathf.Clamp01(clampedValue); //< dummy check
        float equationValue = Mathf.Clamp01(uf.utilityFunction.Evaluate(clampedValue));
        return equationValue * uf.weight;
    }

    /// <summary>
    /// Calculate and return the utility score of color based factor of an action. 
    /// </summary>
    /// <param name="cf"></param>
    /// <returns></returns>
    public float UtilityOfColorFactor(ColorFactor cf)
    {
	    return (cf.Color == _colorManager.PaintColorEnumVal ? cf.ActiveUtility : cf.NotActiveUtility) * cf.weight;
    }

    /// <summary>
    /// Calculates and returns the utility score of an attack
    /// </summary>
    /// <param name="attack"></param>
    /// <param name="currentPhaseNumber">starting from 1</param>
    /// <returns></returns>
    private float UtilityOfAttack(AbstractAttack attack, int currentPhaseNumber)
    {
	    if (!attack)
        {
	        return 0f;
        }
	    attack.lastCalculatedUtility = 0f; //< for debugging
	    // TODO: Weigh repetition with other factors and vice versa
        float numerator = 0f;
        if (attack.maximumConsecutiveUses != 0)
        {
	        var numUses = AttackBufferCountOf(attack);
            // account for utility of repetition
            if (numUses >= attack.maximumConsecutiveUses)
            {
	            return 0f;
            }
            numerator += attack.repUtilFunction.Evaluate((float)numUses / attack.maximumConsecutiveUses);
        }
        // account for all other utility factors
        var factors = attack.GetFactorsForPhase(currentPhaseNumber);
        if (!factors)
        {
	        return 0f;
        }

        var factorSum = CombinedFactorSetUtility(factors);
        if (factorSum == 0f)
        {
	        return 0f;
        }
        numerator += factorSum;
        // combine them
        var u = numerator / (factors.TotalFactors + 1);
        if (u > 0)
        {
	        u += Random.Range(0f, attack.randomUtilBonusMax);
        }
        u = Mathf.Clamp01(u);
        attack.lastCalculatedUtility = u; //< for debugging
        return u;
    }

    /// <summary>
    /// Finds and returns the attack with the best
    /// utility available to this boss
    /// </summary>
    /// <returns></returns>
    private AbstractAttack BestAttack()
    {
        // error check
        if (_attacks.Count < 1)
        {
            return null;
        }
        AbstractAttack bestAttack = _attacks[0];
        int currentPhase = CurrentPhaseIndex + 1;
        float bestUtil = UtilityOfAttack(bestAttack, currentPhase);
        foreach (var at in _attacks)
        {
            if (at == bestAttack)
            {
                continue;
            }
            float util = UtilityOfAttack(at, currentPhase);
            if (util > bestUtil)
            {
                bestAttack = at;
                bestUtil = util;
            }
        }
        _lastCalculatedBestAttack = bestAttack;
        return bestAttack;
    }
    
    #endregion

    /// <summary>
    /// Moves based on the value of the current action being
    /// performed. Called every update
    /// </summary>
    private void MoveBasedOnAction()
    {
        Vector2 moveDir = Vector2.zero;
        bool stopAfterAnimation = false;
        if (CurrentAction)
        {
	        switch (CurrentAction.behavior)
	        {
		        case BossBehavior.MoveToPlayer:
			        if (PaintPathFinder._instance != null)
			        {
				        // update path
				        _pathfindingTimer -= Time.deltaTime;
				        if (_pathfindingTimer <= 0)
				        {
					        ResetPathFind();
				        }
				        // move along path
				        if (_latestPath != null && _latestPath.Count > _pathIndex && _pathIndex > 0)
				        {
					        var nextWorldPos = _latestPath[_pathIndex].getRealWorldPos();
					        moveDir = nextWorldPos - _latestPath[_pathIndex - 1].getRealWorldPos();
					        if (Vector2.Distance(nextWorldPos, transform.position) <= PathNodeCloseRadius)
					        {
						        _pathIndex++;
					        }
					        break;
				        }
			        }
			        // could not find a path, just do default
			        moveDir = _toPlayer.normalized;
			        break;
		        case BossBehavior.Attack:
			        if (_lastCalculatedBestAttack.OverrideMovement)
			        {
				        moveDir = _lastCalculatedBestAttack.OverrideDirection;
				        stopAfterAnimation = true;
			        }
			        break;
		        case BossBehavior.MoveAwayFromPlayer:
			        moveDir = -_toPlayer.normalized;
			        break;
		        case BossBehavior.MoveSpecific:
			        var current = transform.position - _msStartPos;
			        var destination = CurrentAction.relativeDestination;
			        if (current.magnitude < destination.magnitude)
			        {
				        moveDir = destination;
			        }
			        break;
	        }
	        
	        _animator.SetBool(ANIM_WALK, 
		        CurrentAction.behavior != BossBehavior.Attack && moveDir.magnitude > 0);
        }
		_animator.SetFloat(ANIM_DIR_X, moveDir.x);
		_animator.SetFloat(ANIM_DIR_Y, moveDir.y);
        if (stopAfterAnimation || (_phaseEntryBehaviorIsActive && !PerformingAttack))
        {
	        return;
        }
        Vector2 desiredVelocity = moveDir * Acceleration;
        Vector2 trueVelocity = _rigidbody.velocity + desiredVelocity;
        trueVelocity = trueVelocity.normalized * Mathf.Clamp(trueVelocity.magnitude, 0, Speed);
        _rigidbody.velocity = trueVelocity;
    }

    
    /// <summary>
    /// Used to initialize variables related to boss/paint pathfinding
    /// </summary>
    private void ResetPathFind()
    {
	    if (!PaintPathFinder._instance)
	    {
		    return;
	    }
	    _latestPath = PaintPathFinder._instance.CalculatePath(
		    transform.position, _player.transform.position);
	    _pathfindingTimer = PathUpdateRate;
	    _pathIndex = 1;
    }

    /// <summary>
    /// Decides which of this boss's attacks should be used
    /// based on their utility factors. After a decision has been made, the
    /// decided attack is performed
    /// </summary>
    private void Attack()
    {
        // error check
        if (_attacks.Count < 1 || PerformingAttack)
        {
            return;
        }
        if (!_lastCalculatedBestAttack)
        {
            _lastCalculatedBestAttack = BestAttack();
        }
        // perform the attack
        _waitingForAnimatorIdle = true;
        IncrementAttackBuffer(_lastCalculatedBestAttack);
        _lastCalculatedBestAttack.PerformAttack(this, _toPlayer, _player);
        StartCoroutine(AttackInAri());
    }

    /// <summary>
    /// Decides which of this boss's attacks should be used
    /// based on their utility factors. After a decision has been made, the
    /// decided attack is performed
    /// </summary>
    /// <remarks>
    /// This version will update state machine values if possible</remarks>
    /// <param name="attackData"></param>
    private void Attack(AbstractAttack attackData)
    {
	    _lastCalculatedBestAttack = attackData;
	    if (_attackAction)
	    {
		    CurrentAction = _attackAction;
	    }
	    else
	    {
		    Attack(); //< be careful not to make this an infinite loop
	    }
    }

    /// <summary>
    /// Counts and returns the number of times a specified attack
    /// has occurred in the latest attack buffer
    /// </summary>
    /// <param name="attack"></param>
    /// <returns></returns>
    private int AttackBufferCountOf(AbstractAttack attack)
    {
	    int count = 0;
	    foreach (var a in _attackBuffer)
	    {
		    if (a == attack)
		    {
			    count++;
		    }
	    }
	    return count;
    }

    /// <summary>
    /// Add a new attack to the buffer of recently performed attacks
    /// </summary>
    /// <param name="toAdd"></param>
    private void IncrementAttackBuffer(AbstractAttack toAdd)
    {
	    _attackBufferIndex = (_attackBufferIndex + 1) % _attackBuffer.Length;
	    _attackBuffer[_attackBufferIndex] = toAdd;
    }
    
    /// <summary>
    /// Used by animation events to tell the current attack
    /// to enter its active frames
    /// </summary>
    public void TriggerAnimationEvent()
    {
	    if (PerformingAttack)
	    {
		    CurrentAttack.TriggerActiveFrames();
	    }
	    _phaseEntryBehaviorIsActive = false;
    }

    /// <summary>
    /// Used to tell this boss that it's animator has returned
    /// to the idle state
    /// </summary>
    public void TriggerIdle()
    {
	    _waitingForAnimatorIdle = false;
	    if (_lastCalculatedBestAttack)
	    {
		    _lastCalculatedBestAttack.OnIdleEntered();
	    }
	    foreach (var attack in _attacks)
	    {
		    if (attack != _lastCalculatedBestAttack)
		    {
			    attack.OnIdleEntered();
		    }
	    }
    }

    /// <summary>
    /// Account for an amount of damage in the latest
    /// calculation for the rate of damage being taken
    /// by this boss
    /// </summary>
    /// <param name="damage"></param>
    private IEnumerator AffectDRPI(float damage)
    {
	    _totalDamageTakenDuringLatestInterval += damage;
	    yield return new WaitForSeconds(DamageRateInterval);
	    _totalDamageTakenDuringLatestInterval -= damage;
    }
    
    /// <summary>
    /// Account for an attack performed in the latest
    /// calculation for the rate of attacks being performed
    /// by this boss
    /// </summary>
    /// <param name="damage"></param>
    private IEnumerator AttackInAri()
    {
	    _attacksPerformedDuringAri++;
	    yield return new WaitForSeconds(AttackRateInterval);
	    _attacksPerformedDuringAri--;
    }

    /// <summary>
    /// Subscribe to or unsubscribe from events related to
    /// this boss
    /// </summary>
    /// <param name="connected"></param>
    private void ConnectToEvents(bool connected)
    {
	    if (connected)
	    {
		    if (_health)
		    {
			    _health.OnDamage += OnDamage;
		    }
	    }
	    else
	    {
		    if (_health)
		    {
			    _health.OnDamage -= OnDamage;
		    }
	    }
    }

    /// <summary>
    /// Setter for the text displayed above the boss
    /// </summary>
    /// <param name="txt"></param>
    public void SetDebugBehaviorText(string txt)
    {
	    if (_debugBehaviorText)
	    {
		    _debugBehaviorText.text = txt;
	    }
    }

    /// <summary>
    /// Stores and performs behavior when an attack ends
    /// </summary>
    /// <param name="attack">the attack that has just ended</param>
    public void EndOfAttackBehavior(AbstractAttack attack)
    {
	    if (attack.transitioningAttack)
	    {
		    Attack(attack.transitioningAttack);
	    }
	    else if (attack.transitioningAction)
	    {
		    CurrentAction = attack.transitioningAction;
	    }
    }

    /// <summary>
    /// Performs the animation/attack behavior associated with the
    /// pep parameter phase
    /// </summary>
    /// <param name="pep"></param>
    private void PerformPhaseEntryBehavior(PhaseEntrancePair pep)
    {
	    if (pep.EntranceAttack || pep.AnimationString != "")
	    {
		    StartCoroutine(PhaseEntry(pep));
	    }
    }

    /// <summary>
    /// Properly perform actions associated with a phase transition
    /// </summary>
    /// <param name="pep"></param>
    /// <returns></returns>
    private IEnumerator PhaseEntry(PhaseEntrancePair pep)
    {
	    if (_lastCalculatedBestAttack)
	    {
		    _lastCalculatedBestAttack.StopAttackBehavior();
	    }
	    _phaseEntryBehaviorIsActive = true;
	    // ensure we get to the idle state
	    float timeToWait = 1f;
	    _waitingForAnimatorIdle = true;
	    while (_waitingForAnimatorIdle && timeToWait > 0)
	    {
		    timeToWait -= Time.deltaTime;
		    yield return null;
	    }
	    // perform behavior
	    _phaseEntryBehaviorIsActive = true;
	    if (pep.EntranceAttack)
	    {
		    _queuedAttack = pep.EntranceAttack;
		    timeToWait = _minActionSwitchT;
	    }
	    else if (pep.AnimationString != "")
	    {
		    _waitingForAnimatorIdle = true;
		    _animator.SetTrigger(pep.AnimationString);
		    timeToWait = 4f;
	    }
	    while (_phaseEntryBehaviorIsActive && timeToWait > 0)
	    {
		    timeToWait -= Time.deltaTime;
		    yield return null;
	    }
	    _phaseEntryBehaviorIsActive = false;
    }

    /// <summary>
    /// Behavior to perform once this boss is damaged
    /// </summary>
    /// <param name="amount"></param>
    private void OnDamage(float amount)
    {
	    int newIndex = CurrentPhaseIndex;
	    if (newIndex != _lastCalculatedPhaseIndex && 0 <= newIndex && newIndex < _phases.Count)
	    {
		    // enter the new phase
		    PerformPhaseEntryBehavior(_phases[newIndex]);
		    OnPhaseChange?.Invoke(newIndex + 1);
	    }
	    _lastCalculatedPhaseIndex = newIndex;
	    StartCoroutine(AffectDRPI(amount));
    }

    #region Unity Functions

    private void Awake()
    {
        // validate the integrity of all phases
        for (int i = _phases.Count - 1; i >= 0; i--)
        {
            var phase = _phases[i].Phase;
            if (phase == null)
            {
	            _phases.RemoveAt(i);
                continue;
            }
            // be careful, editing scriptable objects here
            for (int x = phase.actions.Count - 1; x >= 0; x--)
            {
	            var action = phase.actions[x];
	            if (action == null)
	            {
		            phase.actions.RemoveAt(x);
		            continue;
	            }
            
	            if (action.maximumUtility < action.minimumUtility)
	            {
		            action.minimumUtility = 0f;
	            }

	            if (action.behavior == BossBehavior.Attack)
	            {
		            _attackAction = action;
	            }
            }
        }
        // ensure we have phases
        if (_phases.Count < 1)
        {
	        Debug.LogError("Boss had no phases (no actions to perform) and has now destroyed itself");
	        if (transform.parent)
	        {
		        Destroy(transform.parent.gameObject);
		        return;
	        }
	        Destroy(gameObject);
	        return;
        }
        

        // Ensure all attacks are valid.
        for (int x = _attacks.Count - 1; x >= 0; x--)
        {
	        var at = _attacks[x];
	        if (at == null)
	        {
		        _attacks.RemoveAt(x);
	        }
        }
        
        // add attacks from the preattack buffer to the attack buffer
        // deincentivising their use on start
        foreach (var attack in _preOccupyAttackBuffer)
        {
	        IncrementAttackBuffer(attack);
        }
        
        ConnectToEvents(true);
        
        if (_debugBehaviorText)
        {
	        _debugBehaviorText.gameObject.SetActive(_debugBehavior);
        }
        
        // ensure phases are ordered by health descending
        _phases.Sort((p1, p2) => p2.Phase.healthLowerBound.CompareTo(p1.Phase.healthLowerBound));
    }

    private void Start()
    {
	    Active = _beginActive;
    }

    // Update is called once per frame
    void Update()
    {
	    if (!_isActive)
	    {
		    return;
	    }
	    
        #region Update utility related variables
        _timeInCurrentAction += Time.deltaTime;
        if (_player)
        {
            _toPlayer = _player.transform.position - transform.position;
        }

        if (PlayerDistance >= _playerCloseDistance)
        {
	        _timeCloseToPlayer -= Time.deltaTime;
        }
        else
        {
	        _timeCloseToPlayer += Time.deltaTime;
        }
        _timeCloseToPlayer = Mathf.Clamp(_timeCloseToPlayer, 0, PlayerCloseTime);
        #endregion
        MoveBasedOnAction();
    }

    private void OnDisable()
    {
	    ConnectToEvents(false);
    }

    private void OnDrawGizmosSelected()
    {
	    Gizmos.color = Color.green;
	    Gizmos.DrawWireSphere(transform.position, _playerCloseDistance);
	    Gizmos.color = Color.red;
	    Gizmos.DrawWireSphere(transform.position, _maximumPlayerDistance);
    }

    #endregion
}

/// <summary>
/// Struct for associating specific behaviors with a boss phase
/// </summary>
[System.Serializable]
public struct PhaseEntrancePair
{
	[Tooltip("Required. Specifies phase data for a boss phase")]
	public BossPhase Phase;
	[Tooltip("If specified, the boss will perform this behavior upon entering this phase " +
	         "(regardless of utility)")]
	public AbstractAttack EntranceAttack;
	[Tooltip("If specified, the boss will activate this animation trigger upon entering this phase, " +
	         "before using any of its attacks or behaviors. Overwritten by Entrance Attack")]
	public string AnimationString;

	public PhaseEntrancePair(BossPhase phase, AbstractAttack entranceBehavior, string animationString, string aq)
	{
		Phase = phase;
		EntranceAttack = entranceBehavior;
		AnimationString = animationString;
	}
}
