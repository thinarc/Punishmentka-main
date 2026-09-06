using _Project.Scripts.Player;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Interactive
{
    public class ItemView : MonoBehaviour
    {
        [Header("Extra")]
        [SerializeField] private Image secondItem;
        [SerializeField] private CanvasGroup buttonForActivateSecondItem;

        public bool skippedBook;

        public bool specialDesk;
        
        public async virtual void SeeView()
        {
            if (skippedBook)
            {
                wait = true;
                await UniTask.Delay(320);
                wait = false;
                return;
            }
            
            Time.timeScale = 0f;
            var g = GetComponent<CanvasGroup>();
            g.blocksRaycasts = true;
            g.interactable = true;
            g.DOFade(1f, 0.4f).SetEase(Ease.OutSine).SetUpdate(true);

            if (!secondItem) return;
            g.interactable = false;
            g.blocksRaycasts = false;
            buttonForActivateSecondItem.interactable = true;
            buttonForActivateSecondItem.blocksRaycasts = true;
            buttonForActivateSecondItem.DOFade(1f, 0.4f).SetEase(Ease.OutSine).SetUpdate(true);
            buttonForActivateSecondItem.GetComponent<Button>().onClick.AddListener(async () =>
            {
                g.interactable = true;
                g.blocksRaycasts = true;
                buttonForActivateSecondItem.interactable = false;
                buttonForActivateSecondItem.blocksRaycasts = false;
                buttonForActivateSecondItem.DOFade(0f, 0.4f).SetEase(Ease.InSine).SetUpdate(true).OnComplete(() =>
                {
                    secondItem.DOFade(1f, 0.4f).SetEase(Ease.OutSine).SetDelay(0.176f).SetUpdate(true);
                });
                SoundManager.PSfx(Resources.Load<AudioClip>("UsableVFx/Fantasy_UI (44)"), 10);
            });
            wait = true;
        }

        protected bool wait;
        public async UniTask WaitEnd()
        {
            wait = true;
            await UniTask.WaitWhile(() => wait);
        }

        public virtual void UnseeView()
        {
            wait = false;
            Time.timeScale = 1f;
            var g = GetComponent<CanvasGroup>();
            g.interactable = false;
            g.DOFade(0f, 0.4f).SetEase(Ease.InSine).OnComplete(() => g.blocksRaycasts = false);
            
            if (!secondItem) return;
            secondItem.DOFade(0f, 0.4f).SetEase(Ease.InSine);
        }
    }
}
