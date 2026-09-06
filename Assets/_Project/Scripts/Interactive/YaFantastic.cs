using _Project.Scripts.EntryPoints;
using _Project.Scripts.Player;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Interactive
{
    [RequireComponent(typeof(Animator))]
    public class YaFantastic : MonoBehaviour
    {
        private static readonly int Shine = Animator.StringToHash("Shine");
        private Animator _anim;

        public bool extra;

        private void Start()
        {
            _anim = GetComponent<Animator>();
            InterItem.doFantasyy += OnFantasy;
        }

        private async void OnFantasy()
        {
            if (!extra)
            {
                await UniTask.Delay(2940);
                _anim.SetTrigger(Shine);
            }
            else
            {
                _anim.SetTrigger(Shine);
                await UniTask.Delay(2940);
                FindAnyObjectByType<GameEntryPoint>().ChangeLight(0.62f);
            }
        }

        private void OnDestroy()
        {
            InterItem.doFantasyy -= OnFantasy;
        }
    }
}