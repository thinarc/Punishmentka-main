using DG.Tweening;
using UnityEngine;

namespace _Project.Scripts
{
    public class TranspTrig : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            GetComponent<SpriteRenderer>().DOFade(0.9f, 0.4f).SetEase(Ease.Linear);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            GetComponent<SpriteRenderer>().DOFade(1, 0.4f).SetEase(Ease.Linear);
        }
    }
}
