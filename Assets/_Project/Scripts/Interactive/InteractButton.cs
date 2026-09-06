using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Scripts.Interactive
{
    public class InteractButton : MonoBehaviour
    {
        public static InteractButton Instance { get; private set; }
        
        private const float ResetDelay = 0.05f;
        private float _resetTimer;

        [ShowInInspector, ReadOnly] private bool _interact;

        public bool Interact
        {
            get
            {
                var ret = _interact;
                _interact = false;
                return ret;
            }
        }

        private void Start()
        {
            Instance = this;
        }

        private void Update()
        {
            _resetTimer -= Time.unscaledDeltaTime;
            if (_resetTimer <= 0 && _interact) _interact = false;
        }
        
        public void PrintWork()
        {
            _interact = true;
            _resetTimer = ResetDelay;
        }
    }
}