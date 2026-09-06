using _Project.Scripts.Player;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts
{
    public class ActiveTrig : MonoBehaviour
    {
        public GameObject[] toActivate;
        public bool invert;

        private bool _stay;

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var state = !invert ? true : false;
            // toActivate[0].SetActive(state);
            // if (toActivate.Length > 1) toActivate[1].SetActive(state);
            PlayerActiveTrig.instance.Activ(toActivate[0].GetComponent<SpriteRenderer>().sortingOrder - 1);
            _stay = true;
        }

        private async void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _stay = false;
            await UniTask.Delay(40);
            if (_stay) return;
            var state = !invert ? false : true;
            PlayerActiveTrig.instance.DeActiv();
            // toActivate[0].SetActive(state);
            // if (toActivate.Length > 1) toActivate[1].SetActive(state);
        }
    }
}
