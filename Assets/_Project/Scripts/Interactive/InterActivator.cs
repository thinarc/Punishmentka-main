using System.Collections.Generic;
using _Project.Scripts.MiniGame;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace _Project.Scripts.Interactive
{
    public class InterActivator : MonoBehaviour
    {
        [SerializeField] private List<InterItem> itemsToActivate;
        private List<InterItem> _itemsToActivate;

        public ItemView waitView;
        public Blockblast special;
        public bool flower;
        public bool teddy;
        public bool door;
        public bool spider;

        private void Awake()
        {
            _itemsToActivate = new List<InterItem>();
            
            if (teddy) return;
            itemsToActivate.ForEach(item =>
            {
                _itemsToActivate.Add(item);
                item.Used += OnUsed;
            });
        }

        private void Update()
        {
            if (teddy && enabled)
            {
                OnUsed(null);
            }

            if (flower && enabled && Mathf.Approximately(GetComponent<SpriteRenderer>().color.a, 1))
            {
                foreach (var t in GetComponentsInChildren<GameObject>())
                {
                    t.SetActive(true);
                }
            }
        }

        public async void OnUsed(InterItem item)
        {
            if (enabled == false && flower) return;
            if (!teddy) item.Used -= OnUsed;
            if (!teddy) itemsToActivate.Remove(item);

            if (itemsToActivate.Count != 0) return;
            
            if (waitView != null) await waitView.WaitEnd();
            
            if (TryGetComponent<Collider2D>(out var coll)) coll.enabled = true;
            if (TryGetComponent<Animator>(out var anim)) anim.enabled = true;
            if (teddy && !door) await UniTask.Delay(940);
            else if (door) await UniTask.Delay(540);
            if (TryGetComponent<InterItem>(out var inter)) inter.enabled = true;
            if (spider) anim.SetTrigger("Shine");
            if (flower && TryGetComponent<SpriteRenderer>(out var sr))
            {
                await sr.DOFade(0, 0).OnComplete(async () =>
                {
                    await sr.DOFade(1, 0.4f).SetEase(Ease.InOutSine).AsyncWaitForCompletion();
                }).AsyncWaitForCompletion();
            }
            if (special && !teddy)
            {
                special.ReInvoke();
            }
            _itemsToActivate.ForEach(i =>
            {
                if (flower)
                {
                    i.enabled = true;
                    i.GetComponent<Collider2D>().enabled = true;
                    enabled = false;
                    return;
                }
                i.enabled = false;
                i.GetComponent<Collider2D>().enabled = false;
            });
        }

        private void OnDestroy() => itemsToActivate.ForEach(item => item.Used -= OnUsed);
    }
}