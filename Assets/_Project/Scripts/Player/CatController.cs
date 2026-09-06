using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts
{
    public sealed class CatController : MonoBehaviour
    {
        private enum State
        {
            Idle,
            Walk,
            GoToFood,
            Eat,
            FollowPlayer,
            CirclePlayer
        }

        [Header("References")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [SerializeField] private Transform player;
        [SerializeField] private Transform foodPoint;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 1.2f;
        [SerializeField] private float foodSpeed = 1.35f;
        [SerializeField] private float followSpeed = 1.45f;

        [SerializeField] private float stoppingDistance = 0.15f;

        [Header("Wander")]
        [SerializeField] private float wanderRadius = 4f;
        [SerializeField] private float minWalkDistance = 1.5f;
        [SerializeField] private float edgePadding = 0.5f;
        [SerializeField] private int randomPointAttempts = 30;

        [Tooltip("Как долго кот обычно гуляет за один раз.")]
        [SerializeField] private Vector2 walkDuration = new(4f, 9f);

        [Header("Idle")]
        [SerializeField] private Vector2 idleTime = new(2f, 5f);

        [Header("Eating")]
        [SerializeField] private Vector2 eatTime = new(3f, 6f);

        [SerializeField, Range(0f, 1f)]
        private float foodDecisionChance = 0.18f;

        [SerializeField, Range(0f, 1f)]
        private float hungerChance = 0.35f;

        [Tooltip("Минимальное время между попытками пойти к еде.")]
        [SerializeField] private float foodCooldown = 15f;

        [Header("Player Interaction")]
        [SerializeField] private float playerNoticeDistance = 4f;
        [SerializeField] private float playerStopDistance = 1f;

        [SerializeField] private float circleDistance = 1.3f;
        [SerializeField] private float circleDuration = 2.5f;

        [SerializeField, Range(0f, 1f)]
        private float playerInterestChance = 0.35f;

        [SerializeField, Range(0f, 1f)]
        private float followChance = 0.55f;

        [SerializeField, Range(0f, 1f)]
        private float circleChance = 0.35f;

        [Header("Player Follow")]
        [SerializeField] private Vector2 followDuration = new(4f, 9f);

        [SerializeField] private float followSideOffset = 0.6f;
        [SerializeField] private float followBackOffset = 0.6f;

        [SerializeField] private float playerLoseDistance = 8f;

        [Header("Decision")]
        [Tooltip("Минимальное время между сменами поведения.")]
        [SerializeField] private Vector2 decisionDelay = new(0.5f, 1.2f);

        [Tooltip("Вероятность остаться Idle после окончания другого поведения.")]
        [SerializeField, Range(0f, 1f)]
        private float idleChance = 0.25f;

        [Tooltip("Как часто кот может попытаться заинтересоваться игроком во время Walk.")]
        [SerializeField] private float playerCheckInterval = 1.5f;

        [Header("NavMesh")]
        [SerializeField] private float navMeshSampleDistance = 1f;
        [SerializeField] private int areaMask = NavMesh.AllAreas;

        [Header("Animation")]
        [SerializeField] private string walkParameter = "Walk";
        [SerializeField] private string idleParameter = "Idle";
        [SerializeField] private string eatParameter = "Eat";

        [Header("Debug")]
        [SerializeField] private bool debugLogs;
        [SerializeField] private bool drawGizmos;

        private State _state;

        private CancellationTokenSource _cts;

        private Vector3 _target;
        private bool _hasTarget;

        private float _circleTimer;
        private float _foodCooldownTimer;
        private float _playerCheckTimer;

        private void Reset()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            SetupAgent();
        }

        private void OnEnable()
        {
            _cts = new CancellationTokenSource();

            SetupAgent();

            _foodCooldownTimer = 0f;
            _playerCheckTimer = 0f;

            Run().Forget();
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            StopMovement();
        }

        private void Update()
        {
            UpdateAnimation();
            UpdateDirection();

            if (_foodCooldownTimer > 0f)
                _foodCooldownTimer -= Time.deltaTime;

            if (_playerCheckTimer > 0f)
                _playerCheckTimer -= Time.deltaTime;
        }

        // =========================================================
        // SETUP
        // =========================================================

        private void SetupAgent()
        {
            if (agent == null)
                return;

            agent.updateRotation = false;
            agent.updateUpAxis = false;

            agent.stoppingDistance = stoppingDistance;
            agent.speed = walkSpeed;

            agent.areaMask = areaMask;
            agent.autoBraking = true;
        }

        // =========================================================
        // MAIN LOOP
        // =========================================================

        private async UniTaskVoid Run()
        {
            await UniTask.Yield();

            ChangeState(State.Idle);

            while (!_cts.IsCancellationRequested)
            {
                switch (_state)
                {
                    case State.Idle:
                        await Idle();
                        break;

                    case State.Walk:
                        await Walk();
                        break;

                    case State.GoToFood:
                        await GoToFood();
                        break;

                    case State.Eat:
                        await Eat();
                        break;

                    case State.FollowPlayer:
                        await FollowPlayer();
                        break;

                    case State.CirclePlayer:
                        await CirclePlayer();
                        break;
                }

                if (_cts.IsCancellationRequested)
                    return;

                await Delay(
                    UnityEngine.Random.Range(
                        decisionDelay.x,
                        decisionDelay.y));

                DecideNextState();
            }
        }

        // =========================================================
        // IDLE
        // =========================================================

        private async UniTask Idle()
        {
            StopMovement();

            float duration = UnityEngine.Random.Range(
                idleTime.x,
                idleTime.y);

            await Delay(duration);
        }

        // =========================================================
        // WALK
        // =========================================================

        private async UniTask Walk()
        {
            /*
             * ВАЖНО:
             * если кот не находится на NavMesh,
             * мы НЕ отправляем его в Idle.
             *
             * Просто заканчиваем это состояние.
             */
            if (agent == null || !agent.isOnNavMesh)
                return;

            if (!TryGetRandomPoint(out Vector3 point))
                return;

            agent.speed = walkSpeed;

            SetDestination(point);

            float duration = UnityEngine.Random.Range(
                walkDuration.x,
                walkDuration.y);

            float elapsed = 0f;

            _playerCheckTimer = UnityEngine.Random.Range(
                0.5f,
                1.5f);

            while (!_cts.IsCancellationRequested &&
                   elapsed < duration)
            {
                elapsed += Time.deltaTime;

                if (ReachedDestination())
                    break;

                /*
                 * Игрок проверяется не каждый кадр.
                 * Поэтому кот не будет постоянно менять состояние.
                 */
                if (_playerCheckTimer <= 0f)
                {
                    _playerCheckTimer = playerCheckInterval;

                    if (ShouldNoticePlayer())
                    {
                        StopMovement();
                        return;
                    }
                }

                /*
                 * Еда тоже проверяется редко.
                 */
                if (foodPoint != null &&
                    _foodCooldownTimer <= 0f &&
                    ShouldGoToFood())
                {
                    StopMovement();

                    ChangeState(State.GoToFood);

                    return;
                }

                await UniTask.Yield(
                    PlayerLoopTiming.Update,
                    _cts.Token);
            }

            StopMovement();
        }

        // =========================================================
        // FOOD
        // =========================================================

        private async UniTask GoToFood()
        {
            if (foodPoint == null)
                return;

            if (agent == null || !agent.isOnNavMesh)
                return;

            if (!TryGetReachablePoint(
                    foodPoint.position,
                    out Vector3 foodPosition))
            {
                /*
                 * НЕ Idle.
                 *
                 * Просто не получилось добраться до еды.
                 * Следующее решение будет принято основным циклом.
                 */
                return;
            }

            agent.speed = foodSpeed;

            SetDestination(foodPosition);

            while (!_cts.IsCancellationRequested)
            {
                if (ReachedDestination())
                    break;

                await UniTask.Yield(
                    PlayerLoopTiming.Update,
                    _cts.Token);
            }

            StopMovement();

            if (_cts.IsCancellationRequested)
                return;

            /*
             * Если дошли до миски —
             * действительно едим.
             */
            if (Vector2.Distance(
                    transform.position,
                    foodPoint.position) <= 1f)
            {
                _foodCooldownTimer = foodCooldown;

                ChangeState(State.Eat);
            }
        }

        private async UniTask Eat()
        {
            StopMovement();

            float duration = UnityEngine.Random.Range(
                eatTime.x,
                eatTime.y);

            await Delay(duration);

            _foodCooldownTimer = foodCooldown;
        }

        // =========================================================
        // FOLLOW PLAYER
        // =========================================================

        private async UniTask FollowPlayer()
        {
            if (player == null)
                return;

            if (agent == null || !agent.isOnNavMesh)
                return;

            agent.speed = followSpeed;

            float duration = UnityEngine.Random.Range(
                followDuration.x,
                followDuration.y);

            float elapsed = 0f;

            while (!_cts.IsCancellationRequested &&
                   elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float distance = Vector2.Distance(
                    transform.position,
                    player.position);

                if (distance > playerLoseDistance)
                    break;

                /*
                 * Кот дошёл достаточно близко.
                 *
                 * Не обязательно сразу менять состояние.
                 * Он может немного постоять возле игрока.
                 */
                if (distance <= playerStopDistance)
                {
                    StopMovement();

                    /*
                     * Иногда начинает кружить.
                     */
                    if (UnityEngine.Random.value < circleChance)
                    {
                        ChangeState(State.CirclePlayer);
                        return;
                    }

                    await Delay(
                        UnityEngine.Random.Range(
                            0.7f,
                            1.8f));

                    continue;
                }

                Vector3 destination =
                    GetFollowDestination();

                if (TryGetReachablePoint(
                        destination,
                        out Vector3 point))
                {
                    SetDestination(point);
                }

                await UniTask.Yield(
                    PlayerLoopTiming.Update,
                    _cts.Token);
            }

            StopMovement();
        }

        private Vector3 GetFollowDestination()
        {
            Vector3 playerForward = Vector3.zero;

            if (player.TryGetComponent(
                    out Rigidbody2D rigidbody2D))
            {
                playerForward =
                    rigidbody2D.linearVelocity;
            }

            if (playerForward.sqrMagnitude < 0.01f)
            {
                if (agent.velocity.sqrMagnitude > 0.01f)
                {
                    playerForward = agent.velocity;
                }
                else
                {
                    playerForward = Vector3.right;
                }
            }

            playerForward.Normalize();

            Vector3 side =
                new Vector3(
                    -playerForward.y,
                    playerForward.x,
                    0f);

            float sideAmount =
                UnityEngine.Random.Range(
                    -followSideOffset,
                    followSideOffset);

            float backAmount =
                UnityEngine.Random.Range(
                    followBackOffset * 0.5f,
                    followBackOffset * 1.5f);

            return
                player.position -
                playerForward * backAmount +
                side * sideAmount;
        }

        // =========================================================
        // CIRCLE PLAYER
        // =========================================================

        private async UniTask CirclePlayer()
        {
            if (player == null)
                return;

            if (agent == null || !agent.isOnNavMesh)
                return;

            agent.speed =
                followSpeed *
                UnityEngine.Random.Range(
                    0.8f,
                    1.1f);

            _circleTimer = 0f;

            float direction =
                UnityEngine.Random.value < 0.5f
                    ? -1f
                    : 1f;

            while (!_cts.IsCancellationRequested &&
                   _circleTimer < circleDuration)
            {
                _circleTimer += Time.deltaTime;

                Vector3 offset =
                    transform.position -
                    player.position;

                if (offset.sqrMagnitude < 0.01f)
                    offset = Vector3.right;

                offset.Normalize();

                Vector3 tangent =
                    new Vector3(
                        -offset.y,
                        offset.x,
                        0f);

                Vector3 desired =
                    (
                        offset +
                        tangent *
                        direction *
                        0.75f
                    ).normalized;

                Vector3 destination =
                    player.position +
                    desired *
                    circleDistance;

                if (TryGetReachablePoint(
                        destination,
                        out Vector3 point))
                {
                    SetDestination(point);
                }

                if (Vector2.Distance(
                        transform.position,
                        player.position) >
                    playerLoseDistance)
                {
                    break;
                }

                await UniTask.Yield(
                    PlayerLoopTiming.Update,
                    _cts.Token);
            }

            StopMovement();
        }

        // =========================================================
        // DECISION
        // =========================================================

        private void DecideNextState()
        {
            /*
             * 1. Сначала проверяем еду.
             *
             * Но только если cooldown закончился.
             */
            if (foodPoint != null &&
                _foodCooldownTimer <= 0f &&
                ShouldGoToFood())
            {
                ChangeState(State.GoToFood);
                return;
            }

            /*
             * 2. Затем игрок.
             */
            if (player != null &&
                IsPlayerInteresting() &&
                UnityEngine.Random.value <= playerInterestChance)
            {
                DecidePlayerBehavior();
                return;
            }

            /*
             * 3. Обычная жизнь.
             *
             * Теперь Walk значительно вероятнее Idle.
             *
             * Это делает кота более живым:
             * он не будет большую часть времени стоять.
             */
            if (UnityEngine.Random.value < idleChance)
            {
                ChangeState(State.Idle);
            }
            else
            {
                ChangeState(State.Walk);
            }
        }

        private void DecidePlayerBehavior()
        {
            float roll = UnityEngine.Random.value;

            /*
             * Большая часть интереса к игроку —
             * следовать за ним.
             */
            if (roll < followChance)
            {
                ChangeState(State.FollowPlayer);
                return;
            }

            /*
             * Иногда — покружить.
             */
            if (roll < followChance + circleChance)
            {
                ChangeState(State.CirclePlayer);
                return;
            }

            /*
             * Иногда просто продолжает гулять.
             */
            ChangeState(State.Walk);
        }

        // =========================================================
        // PLAYER
        // =========================================================

        private bool IsPlayerInteresting()
        {
            if (player == null)
                return false;

            return Vector2.Distance(
                       transform.position,
                       player.position)
                   <=
                   playerNoticeDistance;
        }

        private bool ShouldNoticePlayer()
        {
            if (player == null)
                return false;

            if (!IsPlayerInteresting())
                return false;

            /*
             * Проверка уже ограничена таймером,
             * поэтому здесь шанс можно сделать
             * значительно выше.
             */
            if (UnityEngine.Random.value > 0.25f)
                return false;

            DecidePlayerBehavior();

            return true;
        }

        // =========================================================
        // FOOD DECISION
        // =========================================================

        private bool ShouldGoToFood()
        {
            return
                UnityEngine.Random.value <
                hungerChance * foodDecisionChance;
        }

        // =========================================================
        // NAVMESH
        // =========================================================

        private void SetDestination(Vector3 destination)
        {
            if (agent == null ||
                !agent.isOnNavMesh)
            {
                return;
            }

            if (!NavMesh.SamplePosition(
                    destination,
                    out NavMeshHit hit,
                    navMeshSampleDistance,
                    areaMask))
            {
                return;
            }

            _target = hit.position;
            _hasTarget = true;

            agent.isStopped = false;

            agent.SetDestination(_target);
        }

        private bool TryGetReachablePoint(
            Vector3 destination,
            out Vector3 result)
        {
            result = default;

            if (agent == null ||
                !agent.isOnNavMesh)
            {
                return false;
            }

            if (!NavMesh.SamplePosition(
                    destination,
                    out NavMeshHit hit,
                    navMeshSampleDistance,
                    areaMask))
            {
                return false;
            }

            NavMeshPath path =
                new NavMeshPath();

            if (!agent.CalculatePath(
                    hit.position,
                    path))
            {
                return false;
            }

            if (path.status !=
                NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            result = hit.position;
            return true;
        }

        private bool TryGetRandomPoint(
            out Vector3 result)
        {
            result = default;

            if (agent == null ||
                !agent.isOnNavMesh)
            {
                return false;
            }

            for (int i = 0;
                 i < randomPointAttempts;
                 i++)
            {
                Vector2 random =
                    UnityEngine.Random.insideUnitCircle *
                    wanderRadius;

                Vector3 candidate =
                    transform.position +
                    new Vector3(
                        random.x,
                        random.y,
                        0f);

                if (!NavMesh.SamplePosition(
                        candidate,
                        out NavMeshHit hit,
                        navMeshSampleDistance,
                        areaMask))
                {
                    continue;
                }

                /*
                 * SamplePosition не должен
                 * притягивать точку слишком далеко.
                 */
                if (Vector3.Distance(
                        candidate,
                        hit.position) >
                    navMeshSampleDistance)
                {
                    continue;
                }

                /*
                 * Не выбираем точку рядом с котом.
                 */
                if (Vector3.Distance(
                        transform.position,
                        hit.position) <
                    minWalkDistance)
                {
                    continue;
                }

                /*
                 * Не гуляем прямо по краю NavMesh.
                 */
                if (NavMesh.FindClosestEdge(
                        hit.position,
                        out NavMeshHit edgeHit,
                        areaMask))
                {
                    float distanceToEdge =
                        Vector3.Distance(
                            hit.position,
                            edgeHit.position);

                    if (distanceToEdge <
                        edgePadding)
                    {
                        continue;
                    }
                }

                /*
                 * Проверяем полный путь.
                 */
                NavMeshPath path =
                    new NavMeshPath();

                if (!agent.CalculatePath(
                        hit.position,
                        path))
                {
                    continue;
                }

                if (path.status !=
                    NavMeshPathStatus.PathComplete)
                {
                    continue;
                }

                result = hit.position;
                return true;
            }

            return false;
        }

        private bool ReachedDestination()
        {
            if (!_hasTarget)
                return true;

            if (agent == null ||
                !agent.isOnNavMesh)
            {
                _hasTarget = false;
                return true;
            }

            if (agent.pathPending)
                return false;

            if (agent.remainingDistance <=
                agent.stoppingDistance + 0.05f)
            {
                _hasTarget = false;
                return true;
            }

            /*
             * Если путь стал невалидным —
             * просто прекращаем текущее действие.
             *
             * НЕ превращаем это в Idle().
             */
            if (agent.pathStatus ==
                NavMeshPathStatus.PathInvalid)
            {
                _hasTarget = false;
                return true;
            }

            return false;
        }

        private void StopMovement()
        {
            if (agent == null)
                return;

            if (!agent.isOnNavMesh)
            {
                _hasTarget = false;
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();

            _hasTarget = false;
        }

        // =========================================================
        // ANIMATION
        // =========================================================

        private void UpdateAnimation()
        {
            if (animator == null)
                return;

            /*
             * У нас теперь ТРИ bool:
             *
             * Walk
             * Idle
             * Eat
             *
             * Поэтому Animator сам решает,
             * какую анимацию показывать.
             */

            bool isWalking =
                _state == State.Walk ||
                _state == State.GoToFood ||
                _state == State.FollowPlayer ||
                _state == State.CirclePlayer;

            bool isEating =
                _state == State.Eat;

            bool isIdle =
                _state == State.Idle;

            animator.SetBool(
                walkParameter,
                isWalking);

            animator.SetBool(
                idleParameter,
                isIdle);

            animator.SetBool(
                eatParameter,
                isEating);
        }

        // =========================================================
        // DIRECTION
        // =========================================================

        private void UpdateDirection()
        {
            if (spriteRenderer == null ||
                agent == null)
            {
                return;
            }

            float x =
                agent.velocity.x;

            if (Mathf.Abs(x) < 0.05f)
                return;

            /*
             * В твоём спрайте:
             *
             * X > 0 -> кот смотрит ВЛЕВО
             * X < 0 -> кот смотрит ВПРАВО
             */
            spriteRenderer.flipX =
                x > 0f;
        }

        // =========================================================
        // STATE
        // =========================================================

        private void ChangeState(State newState)
        {
            if (_state == newState)
                return;

            State previous =
                _state;

            _state = newState;

            if (debugLogs)
            {
                Debug.Log(
                    $"[Cat] {previous} -> {newState}",
                    this);
            }
        }

        // =========================================================
        // PUBLIC
        // =========================================================

        public void ForceEat()
        {
            if (foodPoint == null)
                return;

            _foodCooldownTimer = 0f;

            ChangeState(State.GoToFood);
        }

        public void CircleAroundPlayer()
        {
            if (player == null)
                return;

            ChangeState(State.CirclePlayer);
        }

        // =========================================================
        // UTILITY
        // =========================================================

        private async UniTask Delay(float seconds)
        {
            if (seconds <= 0f)
                return;

            await UniTask.Delay(
                TimeSpan.FromSeconds(seconds),
                cancellationToken: _cts.Token);
        }

        // =========================================================
        // GIZMOS
        // =========================================================

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
                return;

            Gizmos.DrawWireSphere(
                transform.position,
                wanderRadius);

            Gizmos.DrawWireSphere(
                transform.position,
                playerNoticeDistance);

            Gizmos.DrawWireSphere(
                transform.position,
                circleDistance);

            if (_hasTarget)
            {
                Gizmos.DrawLine(
                    transform.position,
                    _target);

                Gizmos.DrawWireSphere(
                    _target,
                    0.1f);
            }
        }
    }
}