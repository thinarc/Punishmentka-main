using System;
using _Project.Scripts.Player;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Interactive
{
    public class InterItem : MonoBehaviour
    {
        [SerializeField] private bool act;
        public bool ciga;
        
        [SerializeField] private Material extra;
        [SerializeField] private Material extraS;
        public bool oneshot;
        public bool oneshotspecial;
        
        private Material _selected;
        private Material _selectedS;
        private Material _def;
        
        private SpriteRenderer _sprite;
        private SpriteRenderer Sprite
        {
            set => _sprite = value;
            get { return appliedSprite == null ? _sprite : appliedSprite; }
        }
        [SerializeField] private SpriteRenderer appliedSprite;
        private Animator _anim;

        public static InterItem Stayed;
        public static InterItem Stayed2;
        private static readonly int Interact = Animator.StringToHash("Interact");

        private bool _used;
        public event Action<InterItem> Used;

        [Header("Extra")]
        [SerializeField] private bool realFantasy;
        [SerializeField] private ItemView uiView;

        public static event Action doFantasyy;

        [Space(10)] public bool black;
        public bool cigaaa;
        public bool recRecord;
        public AudioClip[] clip;
        public int vol = 2;

        public void NowOneshot()
        {
            if (oneshot) return;
            oneshot = true;
            var path = "Outline";
            if (black) path = "BlackS/Outline";
            _selected = Resources.Load<Material>(path);
            _selectedS = Resources.Load<Material>(path + "S");
            _def = Resources.Load<Material>("Sprite-Lit-Default");
            if (extra != null) _selected = extra;
            if (extraS != null) _selectedS = extraS;
            
            Sprite.material = _selected;
            Stayed = null;
            Stayed2 = null;
            _used = false;
        }

        private void Start()
        {
            if (!cigaaa) Sprite = GetComponent<SpriteRenderer>();
            else Sprite = GetComponentInParent<SpriteRenderer>();
            _anim = GetComponent<Animator>();
            
            
            var path = "Outline";
            if (black) path = "BlackS/Outline";
            _selected = Resources.Load<Material>(path);
            _selectedS = Resources.Load<Material>(path + "S");
            _def = Resources.Load<Material>("Sprite-Lit-Default");
            if (extra != null) _selected = extra;
            if (extraS != null) _selectedS = extraS;
            
            Sprite.material = _selected;
        }
        
        private void Update()
        {
            if (act && enabled) _anim.SetTrigger("Act");
            
            if (_used) _selected = _def;
            if (_used) _selectedS = _def;
        }

        private bool enter;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (enter) return;
            enter = true;
            if (!enabled) return;
            
            if (!other.CompareTag("Player")) return;
            if ((oneshot || oneshotspecial) && _used) return;
            if (Stayed != null) Stayed2 = Stayed;
            Stayed = this;
        }
        
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!enter) return;
            enter = false;
            if (!enabled) return;
            
            if (!other.CompareTag("Player")) return;
            Sprite.material = _selected;
            if (Stayed == this) Stayed = null;
            if (Stayed2 == this) Stayed2 = null;
            else if (Stayed2 != null) Stayed = Stayed2;
            Stayed2 = null;
        }

        public bool extr;
        
        private async void OnTriggerStay2D(Collider2D other)
        {
            if (!enabled) return;
            
            if (!other.CompareTag("Player")) return;
            if ((oneshot || oneshotspecial) && _used)
            {
                GetComponent<Collider2D>().enabled = false;
                enabled = false;
                return;
            }

            if (Stayed != this)
            {
                Sprite.material = _selected;
                return;
            }
            Sprite.material = _selectedS;
            
            if (!InteractButton.Instance.Interact) return;
            if (extr)
            {
                if (clip.Length == 0) return;
                SoundManager.PSfx(clip?[0], vol);
                if (clip.Length >= 2)
                {
                    // await UniTask.Delay(400);
                    SoundManager.PSfx(clip?[1], vol);
                }
                if (clip.Length == 3)
                {
                    await UniTask.Delay(1200);
                    SoundManager.PSfx(clip?[2], vol);
                }
            }
            _anim.SetTrigger(Interact);
            Sprite.material = _def;
            _used = true;
            Used?.Invoke(this);
            FindAnyObjectByType<PlayerMovement>().ResetTarget();
            if (realFantasy) DoFantasy();
            if (ciga)
            {
                FindAnyObjectByType<WebGLVideoUI>().PlayVideo();
            }
            if (manualS)
            {
                await UniTask.Delay(1400);
                var sound = SoundManager.instance;
                if (sound.bg.clip.name != "Final") sound.PlayClipManual(Resources.Load<AudioClip>("Final"));
            }
            if (recRecord) FindAnyObjectByType<PlayerAnim>().OneFantasy();
            if (!extr)
            {
                if (clip.Length == 0) return;
                SoundManager.PSfx(clip?[0], vol);
                if (clip.Length >= 2)
                {
                    // await UniTask.Delay(400);
                    SoundManager.PSfx(clip?[1], vol);
                }
                if (clip.Length == 3)
                {
                    await UniTask.Delay(1200);
                    SoundManager.PSfx(clip?[2], vol);
                }
            }
        }

        public bool manualS;

        private void DoFantasy()
        {
            SoundManager.instance.ReturnPlayClip(Resources.Load<AudioClip>("RealFantasy"));
            doFantasyy?.Invoke();
        }

        public void KeySeeUI() => uiView.SeeView();
    }
}