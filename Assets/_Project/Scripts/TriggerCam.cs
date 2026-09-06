using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Scripts
{
    [ExecuteInEditMode]
    public class TriggerCam : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private float smooth;

        [ShowInInspector] private float _targetPos;
        [SerializeField] private Transform target;

        private void Start() => _targetPos = -1.2f;

        private void LateUpdate()
        {
            var pos = target.transform.position;
            pos.x = Mathf.MoveTowards(pos.x, _targetPos, smooth * Time.deltaTime);
            target.transform.position = pos;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _targetPos = 1.2f;
            }
        }
        
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _targetPos = -1.2f;
            }
        }
    }
}
