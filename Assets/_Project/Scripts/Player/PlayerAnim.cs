using Cysharp.Threading.Tasks;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _Project.Scripts.Player
{
    public class PlayerAnim : MonoBehaviour
    {
        [SerializeField] private AnimatorOverrideController[] overrides;
        private RuntimeAnimatorController _controller;

        [SerializeField] private float idleMulti = 1;
        [SerializeField] private float walkMulti = 1;
        
        private Animator _anim;
        private PlayerMovement _player;

        [SerializeField] private RuntimeAnimatorController start;
        public int _current;

        public RuntimeAnimatorController fStart;
        public AnimatorOverrideController[] fOverrides;
        public Light2D[] ligtsF;
        public float[] ligthsFSt;
        
        public AudioClip[] footSounds;

        public async void OneFantasy()
        {
            start = fStart;
            _controller = fStart;
            overrides = fOverrides;

            await UniTask.WaitWhile(() =>
            {
                for (var i = 0; i < ligtsF.Length; i++)
                {
                    ligtsF[i].intensity = Mathf.MoveTowards(ligtsF[i].intensity, ligthsFSt[i], Time.deltaTime);
                }
                return !Mathf.Approximately(ligtsF[0].intensity, ligthsFSt[0]) || !Mathf.Approximately(ligtsF[2].intensity, ligthsFSt[2]);
            });

            await UniTask.Delay(600);
            await UniTask.Delay(200);
            
            while (true)
            {
                var rand = ligthsFSt[0] * Random.Range(0.64f, 1.24f);
                
                await UniTask.WaitUntil(() =>
                {
                    var done = true;

                    for (int i = 0; i < ligtsF.Length; i++)
                    {
                        ligtsF[i].intensity = Mathf.MoveTowards(
                            ligtsF[i].intensity,
                            rand,
                            Time.deltaTime * 0.6f);

                        if (!Mathf.Approximately(ligtsF[i].intensity, rand))
                            done = false;
                    }

                    return done;
                });
                
                var rand2 = ligthsFSt[2] * Random.Range(0.64f, 1.24f);
                
                await UniTask.WaitUntil(() =>
                {
                    var done = true;

                    for (int i = 0; i < ligtsF.Length; i++)
                    {
                        ligtsF[i].intensity = Mathf.MoveTowards(
                            ligtsF[i].intensity,
                            rand2,
                            Time.deltaTime * 0.4f);

                        if (!Mathf.Approximately(ligtsF[i].intensity, rand2))
                            done = false;
                    }

                    return done;
                });
            }
        }

        private void Start()
        {
            _anim = GetComponent<Animator>();
            _player = GetComponentInParent<PlayerMovement>();
            
            footSounds = Resources.LoadAll<AudioClip>("UsableVFx/steps");
            if (transform.parent.name == "Player3") footSounds = Resources.LoadAll<AudioClip>("UsableVFx/steps2");
            
            if (start == null) _controller = _anim.runtimeAnimatorController;
            else _controller = start;
            
            for (var i = 0; i < ligtsF.Length; i++)
            {
                ligthsFSt[i] = ligtsF[i].intensity;
                ligtsF[i].intensity = 0;
            }
        }

        private bool footPlay;

        private async void Update()
        {
            var velocity = _player.Velocity;
            _anim.SetFloat("idlemulti", idleMulti);
            _anim.SetBool("walk", velocity.magnitude > 0.034f);
            if (velocity.magnitude > 0.034f && !footPlay)
            {
                SoundManager.PSfx(footSounds[Random.Range(0, footSounds.Length)], Random.Range(3, 5));
                footPlay = true;
                if (transform.parent.name == "Player3") await UniTask.Delay(840);
                else await UniTask.Delay(540);

                footPlay = false;
            }
            
            var acceleration = velocity.magnitude;
            if (acceleration < 0.24f) acceleration = 0.24f;
            acceleration *= 1.56f;
            if (acceleration > 0.99f) acceleration = 0.99f;
            _anim.SetFloat("walkmulti", walkMulti * acceleration);
            
            if (velocity.magnitude <= 0.034f) return;
            if (Mathf.Abs(_player.Velocity.x) > Mathf.Abs(_player.Velocity.y))
            {
                if (velocity.x > 0 && _current != 3) ChangeController(3);
                else if (velocity.x < 0 && _current != 2) ChangeController(2);
            }
            else if (Mathf.Abs(_player.Velocity.x) < Mathf.Abs(_player.Velocity.y))
            {
                if (velocity.y > 0 && _current != 1) ChangeController(1);
                else if (velocity.y < 0 && _current != 0) ChangeController(0);
            }
        }

        private void ChangeController(int? state = null)
        {
            if (state != null) _current = state.Value;
            var controller = state != 0 ? overrides[_current - 1] : _controller;
            
            if (_anim.runtimeAnimatorController == controller) return;
            _anim.runtimeAnimatorController = controller;
        }
    }
}
