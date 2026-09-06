using System;
using _Project.Scripts.Player;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.EntryPoints
{
    public class MenuInterface : MonoBehaviour
    {
        public static MenuInterface instance;
        private void Awake() => instance = this;
        
        public MeshRenderer texture;
        public Material blur;
        public Animator studyButton;
        public GameObject[] upLayers;

        private float inkSpread = 15f;
        private bool inkSpeadAnim;

        public CanvasGroup menu;
        public RectTransform menuRect;
        public CanvasGroup logo;
        public RectTransform logoRect;
        public RectTransform buttonRect;

        private void BeforeMenu()
        {
            upLayers.ForEach(g => g.SetActive(false));
            texture.sharedMaterial.SetFloat("_InkSpreadDistance", inkSpread);
            texture.sharedMaterial.SetFloat("_FadingFade", 1);
            texture.sharedMaterial.SetFloat("_GaussianBlurFade", 1);
        }

        private void Update()
        {
            if (inkSpeadAnim)
            {
                inkSpread = Mathf.Lerp(inkSpread, -5f, 1.001f - Mathf.Exp(-0.2f * Time.unscaledDeltaTime));
                texture.sharedMaterial.SetFloat("_InkSpreadDistance", inkSpread);
            }
        }

        public async UniTask StartMenu()
        {
            texture.material = blur;
            
            logoRect.DOScale(0.6f, 0);
            menu.DOFade(0, 0);
            logo.DOFade(0, 0);
            menu.interactable = true;
            menu.blocksRaycasts = true;
            buttonRect.GetComponent<Button>().interactable = false;
            BeforeMenu();
            logoRect.DOScale(1, 0.64f).SetEase(Ease.OutBack);
            await logo.DOFade(1, 0.8f).SetEase(Ease.InOutSine).AsyncWaitForCompletion();
            menu.DOFade(1, 0.44f).SetEase(Ease.InOutSine);
            buttonRect.GetComponent<Button>().interactable = true;

            await UniTask.WaitWhile(() => menu.blocksRaycasts);
        }

        public async UniTask UndoMaterial()
        {
            await UniTask.WaitWhile(() =>
            {
                var val = texture.sharedMaterial.GetFloat("_FadingFade");
                val = Mathf.MoveTowards(val, 0, 1f * Time.deltaTime);
                texture.sharedMaterial.SetFloat("_FadingFade", val);
                return val != 0;
            });
            await UniTask.WaitWhile(() =>
            {
                var val = texture.sharedMaterial.GetFloat("_GaussianBlurFade");
                val = Mathf.MoveTowards(val, 0, 1f * Time.deltaTime);
                texture.sharedMaterial.SetFloat("_GaussianBlurFade", val);
                return val != 0;
            });
        }

        public async void UndoMenu(Material start)
        {
            SoundManager.PSfx(Resources.Load<AudioClip>("UsableVFx/Trimmed_sound-effect-4351"), 0);
            
            var tween = buttonRect.DOShakePosition(0.1f, 3f).SetLoops(-1).SetEase(Ease.OutBack);
            buttonRect.GetComponent<Button>().interactable = false;
            
            await UniTask.Delay(400);
            inkSpeadAnim = true;
            await UniTask.Delay(200);
            logo.DOFade(0, 0.74f).SetEase(Ease.InOutSine);
            await UniTask.Delay(TimeSpan.FromSeconds(0.4f));
            menu.DOFade(0, 0.34f).SetEase(Ease.InOutSine);

            await UndoMaterial();
            texture.sharedMaterial = start;
            var player = upLayers[0];
            upLayers[0] = null;
            upLayers.ForEach(g =>
            {
                if (g == null) return;
                var sprite = g.GetComponentInChildren<SpriteRenderer>();
                sprite.DOFade(0, 0);
                g.SetActive(true);
                sprite.DOFade(1, 0.4f).SetEase(Ease.InOutSine);
            });
            
            studyButton.SetTrigger("Study");
            await UniTask.Delay(500);
            
            var sprite = player.GetComponentInChildren<SpriteRenderer>();
            sprite.DOFade(0, 0);
            player.SetActive(true);
            player.GetComponent<PlayerMovement>().disable = true;
            sprite.DOFade(1, 0.4f).SetEase(Ease.InOutSine);
            
            await UniTask.Delay(500);
            menu.blocksRaycasts = false;
            menu.interactable = false;
            player.GetComponent<PlayerMovement>().disable = false;
        }
    }
}