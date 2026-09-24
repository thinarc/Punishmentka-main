using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts
{
    /// <summary>
    /// Presentation layer for the cat: drives the Animator bools and flips the sprite
    /// based on actual NavMeshAgent movement. Kept separate from <see cref="CatController"/>
    /// so the FSM/movement logic doesn't have to know anything about animation.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class CatAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CatController cat;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Animator Parameters")]
        [SerializeField] private string walkParameter = "Walk";
        [SerializeField] private string idleParameter = "Idle";
        [SerializeField] private string eatParameter = "Eat";

        [Header("Facing")]
        [Tooltip("Ниже этой скорости направление взгляда не меняется (кот не 'дёргается' на месте).")]
        [SerializeField] private float facingVelocityThreshold = 0.05f;

        private void Reset()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            cat = GetComponentInParent<CatController>();
            agent = GetComponentInParent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            if (cat == null)
                return;

            cat.StateChanged += HandleStateChanged;
            HandleStateChanged(cat.State);
        }

        private void OnDisable()
        {
            if (cat != null)
                cat.StateChanged -= HandleStateChanged;
        }

        private void Update()
        {
            UpdateFacing();
            UpdateMovementParameters();
        }

        private void HandleStateChanged(CatController.CatState state)
        {
            // Animator bools are fully driven every frame from actual movement/arrival
            // in UpdateMovementParameters() — this callback exists only so CatAnimator
            // can react to state changes if presentation logic needs it later.
        }

        private void UpdateMovementParameters()
        {
            if (animator == null || cat == null)
                return;

            // IsEatingNow only becomes true once the cat has actually arrived at the
            // bowl and stopped — while it's still walking there, this stays false so
            // the Walk animation keeps playing instead of "eating from thin air".
            bool isEating = cat.IsEatingNow;
            bool isMoving = !isEating && agent != null && agent.velocity.sqrMagnitude > facingVelocityThreshold * facingVelocityThreshold;

            animator.SetBool(eatParameter, isEating);
            animator.SetBool(walkParameter, isMoving);
            animator.SetBool(idleParameter, !isEating && !isMoving);
        }

        private void UpdateFacing()
        {
            if (spriteRenderer == null || agent == null)
                return;

            float x = agent.velocity.x;

            if (Mathf.Abs(x) < facingVelocityThreshold)
                return;

            // Sprite art: X > 0 (moving right) -> cat faces LEFT, hence flipX = x > 0.
            spriteRenderer.flipX = x > 0f;
        }
    }
}
