using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace _Project.Scripts
{
    /// <summary>
    /// Cat "brain": a small finite state machine driving a NavMeshAgent.
    /// Presentation (Animator parameters, sprite flip) lives in <see cref="CatAnimator"/>,
    /// which listens to <see cref="StateChanged"/> instead of being driven from here.
    /// </summary>
    public sealed class CatController : MonoBehaviour
    {
        public enum CatState
        {
            Patrolling,
            Eating,
            OrbitingGirl
        }

        [Header("References")]
        [SerializeField] private NavMeshAgent agent;

        [FormerlySerializedAs("player")]
        [Tooltip("Трансформ девочки, вокруг которой кот иногда крутится.")]
        [SerializeField] private Transform girl;

        [Tooltip("Точка миски с едой.")]
        [SerializeField] private Transform foodPoint;

        [Header("Movement")]
        [FormerlySerializedAs("walkSpeed")]
        [SerializeField] private float moveSpeed = 1.2f;
        [SerializeField] private float stoppingDistance = 0.15f;
        [SerializeField] private float navMeshSampleDistance = 1f;

        [Header("Patrol (по умолчанию)")]
        [FormerlySerializedAs("wanderRadius")]
        [SerializeField] private float patrolRadius = 4f;
        [FormerlySerializedAs("minWalkDistance")]
        [SerializeField] private float minPatrolDistance = 1.5f;
        [FormerlySerializedAs("randomPointAttempts")]
        [SerializeField] private int patrolPointAttempts = 30;

        [Tooltip("Сколько кот стоит на месте между перемещениями (мин/макс).")]
        [FormerlySerializedAs("idleTime")]
        [SerializeField] private Vector2 patrolPauseDuration = new(2f, 5f);

        [Header("Кормление")]
        [FormerlySerializedAs("eatTime")]
        [Tooltip("Сколько длится анимация поедания (мин/макс).")]
        [SerializeField] private Vector2 eatDuration = new(3f, 6f);

        [Tooltip("Через сколько секунд кот снова захочет пойти к еде (мин/макс).")]
        [SerializeField] private Vector2 feedingInterval = new(20f, 40f);

        [Header("Внимание к девочке")]
        [FormerlySerializedAs("playerNoticeDistance")]
        [Tooltip("Дистанция, на которой кот замечает девочку и может подойти к ней.")]
        [SerializeField] private float attentionRange = 6f;

        [Tooltip("Через сколько секунд кот снова захочет уделить внимание девочке (мин/макс).")]
        [SerializeField] private Vector2 attentionInterval = new(15f, 30f);

        [FormerlySerializedAs("circleDistance")]
        [SerializeField] private float orbitRadius = 1.3f;
        [FormerlySerializedAs("circleDuration")]
        [SerializeField] private float orbitDuration = 4f;
        [SerializeField] private float orbitAngularSpeed = 90f;
        [SerializeField] private float girlLoseDistance = 8f;

        [Header("Debug")]
        [SerializeField] private bool debugLogs;
        [SerializeField] private bool drawGizmos = true;

        private const float ArrivalTolerance = 0.05f;

        /// <summary>Raised whenever the state changes, so presentation code (CatAnimator) can react.</summary>
        public event Action<CatState> StateChanged;

        public CatState State { get; private set; }

        /// <summary>
        /// True only once the cat has actually arrived at the food and stopped —
        /// while it's still walking towards the bowl, this stays false so the
        /// eating animation doesn't start early.
        /// </summary>
        public bool IsEatingNow { get; private set; }

        private bool _isPausing;
        private float _patrolPauseTimer;
        private float _feedingTimer;
        private float _attentionTimer;
        private float _stateTimer;
        private float _orbitAngle;
        private float _orbitDirection;

        private void Reset()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Awake()
        {
            ConfigureAgent();
        }

        private void OnEnable()
        {
            _feedingTimer = UnityEngine.Random.Range(feedingInterval.x, feedingInterval.y);
            _attentionTimer = UnityEngine.Random.Range(attentionInterval.x, attentionInterval.y);

            EnterState(CatState.Patrolling);
        }

        private void OnDisable()
        {
            StopAgent();
        }

        private void Update()
        {
            TickTimer(ref _feedingTimer);
            TickTimer(ref _attentionTimer);

            switch (State)
            {
                case CatState.Patrolling:
                    UpdatePatrolling();
                    break;

                case CatState.Eating:
                    UpdateEating();
                    break;

                case CatState.OrbitingGirl:
                    UpdateOrbitingGirl();
                    break;
            }
        }

        // =========================================================
        // PATROLLING (default state)
        // =========================================================

        private void EnterPatrolling()
        {
            // Timer at 0 makes the very first Update() pick a point immediately.
            _isPausing = true;
            _patrolPauseTimer = 0f;
        }

        private void UpdatePatrolling()
        {
            if (TryInterruptForFood())
                return;

            if (TryInterruptForGirl())
                return;

            if (_isPausing)
            {
                _patrolPauseTimer -= Time.deltaTime;

                if (_patrolPauseTimer > 0f)
                    return;

                if (!TryFindPatrolPoint(out Vector3 point))
                {
                    // Couldn't sample a valid point this time — retry shortly instead of
                    // hammering NavMesh.SamplePosition every single frame.
                    _patrolPauseTimer = 0.5f;
                    return;
                }

                _isPausing = false;
                SetDestination(point);
                return;
            }

            if (HasArrived())
            {
                StopAgent();
                _isPausing = true;
                _patrolPauseTimer = UnityEngine.Random.Range(patrolPauseDuration.x, patrolPauseDuration.y);
            }
        }

        private bool TryFindPatrolPoint(out Vector3 result)
        {
            result = default;

            if (agent == null || !agent.isOnNavMesh)
                return false;

            for (int i = 0; i < patrolPointAttempts; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * patrolRadius;
                Vector3 candidate = transform.position + new Vector3(offset.x, offset.y, 0f);

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
                    continue;

                if (Vector3.Distance(transform.position, hit.position) < minPatrolDistance)
                    continue;

                NavMeshPath path = new NavMeshPath();

                if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
                    continue;

                result = hit.position;
                return true;
            }

            return false;
        }

        // =========================================================
        // EATING
        // =========================================================

        private bool TryInterruptForFood()
        {
            if (foodPoint == null || _feedingTimer > 0f)
                return false;

            EnterState(CatState.Eating);
            return true;
        }

        private void EnterEating()
        {
            _stateTimer = -1f; // negative = "still walking to the bowl"
            IsEatingNow = false;
            SetDestination(foodPoint.position);
        }

        private void UpdateEating()
        {
            if (foodPoint == null)
            {
                EnterState(CatState.Patrolling);
                return;
            }

            // Still on the way to the bowl.
            if (_stateTimer < 0f)
            {
                if (!HasArrived())
                    return;

                StopAgent();
                _stateTimer = UnityEngine.Random.Range(eatDuration.x, eatDuration.y);

                // Trigger Eat Animation
                IsEatingNow = true;
                return;
            }

            _stateTimer -= Time.deltaTime;

            if (_stateTimer <= 0f)
            {
                _feedingTimer = UnityEngine.Random.Range(feedingInterval.x, feedingInterval.y);
                EnterState(CatState.Patrolling);
            }
        }

        // =========================================================
        // ORBITING GIRL
        // =========================================================

        private bool TryInterruptForGirl()
        {
            if (girl == null || _attentionTimer > 0f)
                return false;

            if (DistanceTo(girl.position) > attentionRange)
                return false;

            EnterState(CatState.OrbitingGirl);
            return true;
        }

        private void EnterOrbiting()
        {
            _stateTimer = orbitDuration;
            _orbitAngle = UnityEngine.Random.Range(0f, 360f);
            _orbitDirection = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        }

        private void UpdateOrbitingGirl()
        {
            if (girl == null || DistanceTo(girl.position) > girlLoseDistance)
            {
                _attentionTimer = UnityEngine.Random.Range(attentionInterval.x, attentionInterval.y);
                EnterState(CatState.Patrolling);
                return;
            }

            _stateTimer -= Time.deltaTime;

            if (_stateTimer <= 0f)
            {
                _attentionTimer = UnityEngine.Random.Range(attentionInterval.x, attentionInterval.y);
                EnterState(CatState.Patrolling);
                return;
            }

            _orbitAngle += orbitAngularSpeed * _orbitDirection * Time.deltaTime;

            Vector3 offset = new Vector3(
                Mathf.Cos(_orbitAngle * Mathf.Deg2Rad),
                Mathf.Sin(_orbitAngle * Mathf.Deg2Rad),
                0f) * orbitRadius;

            SetDestination(girl.position + offset);
        }

        // =========================================================
        // STATE
        // =========================================================

        private void EnterState(CatState newState)
        {
            State = newState;
            IsEatingNow = false;

            switch (newState)
            {
                case CatState.Patrolling:
                    EnterPatrolling();
                    break;

                case CatState.Eating:
                    EnterEating();
                    break;

                case CatState.OrbitingGirl:
                    EnterOrbiting();
                    break;
            }

            if (debugLogs)
                Debug.Log($"[Cat] -> {newState}", this);

            StateChanged?.Invoke(newState);
        }

        // =========================================================
        // NAVMESH HELPERS
        // =========================================================

        private void ConfigureAgent()
        {
            if (agent == null)
                return;

            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.speed = moveSpeed;
            agent.stoppingDistance = stoppingDistance;
            agent.autoBraking = true;
        }

        private void SetDestination(Vector3 worldPosition)
        {
            if (agent == null || !agent.isOnNavMesh)
                return;

            if (!NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
                return;

            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }

        private void StopAgent()
        {
            if (agent == null || !agent.isOnNavMesh)
                return;

            agent.isStopped = true;
            agent.ResetPath();
        }

        private bool HasArrived()
        {
            if (agent == null || !agent.isOnNavMesh)
                return true;

            if (agent.pathPending)
                return false;

            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
                return true;

            return agent.remainingDistance <= agent.stoppingDistance + ArrivalTolerance;
        }

        private float DistanceTo(Vector3 point)
        {
            return Vector2.Distance(transform.position, point);
        }

        private static void TickTimer(ref float timer)
        {
            if (timer > 0f)
                timer -= Time.deltaTime;
        }

        // =========================================================
        // PUBLIC API (e.g. from other gameplay signals/cutscenes)
        // =========================================================

        /// <summary>Interrupt whatever the cat is doing and send it to the food bowl right now.</summary>
        public void ForceFeeding()
        {
            if (foodPoint == null)
                return;

            EnterState(CatState.Eating);
        }

        /// <summary>Interrupt whatever the cat is doing and make it pay attention to the girl right now.</summary>
        public void ForceAttention()
        {
            if (girl == null)
                return;

            EnterState(CatState.OrbitingGirl);
        }

        // =========================================================
        // GIZMOS
        // =========================================================

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, patrolRadius);

            if (girl != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, attentionRange);
                Gizmos.DrawWireSphere(girl.position, orbitRadius);
            }
        }
    }
}
