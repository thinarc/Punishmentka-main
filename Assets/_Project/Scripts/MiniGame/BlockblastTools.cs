using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Scripts.MiniGame
{
    public class BlockblastTools : MonoBehaviour
    {
        [SerializeField] private Blockblast blockblast;
        
        [Header("Debug")]
        [ShowInInspector, Space(5)] private List<BlockblastKey> _keys;

        private List<UniTask> _awaits;
        private List<GameObject> _insts;
        private async void AwaitCatch(UniTask task)
        {
            _awaits.Add(task);
            await task;
            _awaits.Remove(task);
        }

        public void ResetKeys()
        {
            _insts = null;
            _waiting = false;
            _keys = null;
            spawned.Clear();
        }
        
        public async UniTask InitKeys(int area)
        {
            if (_insts != null && _insts?.Count != 0)
            {
                _insts.ForEach(i => Destroy(i));
                _insts.Clear();
            }
            _awaits = new List<UniTask>();
            _insts ??= new List<GameObject>();
            
            if (area is > 12 or < 1) throw new Exception("Area out of range: " + area);
            _keys ??= GetComponentsInChildren<BlockblastKey>(true).Where(k => !k.gameObject.activeSelf).ToList();
            var keysBox = _keys.Where(k => k.BlockForm == BlockForm.Box).ToList();
            var keysPoint = _keys.Where(k => k.BlockForm == BlockForm.Point).ToList();
            var keysL = _keys.Where(k => k.BlockForm is BlockForm.LUpLeft or BlockForm.LUpRight or BlockForm.LDownRight or BlockForm.LDownLeft).ToList();
            var keysI = _keys.Where(k => k.BlockForm is BlockForm.IHorizontal or BlockForm.IVertical).ToList();
            CalcUniqueForms(area, out var boxes, out var lforms, out var iforms, out var points);

            for (; boxes > 0; boxes--)
                SpawnFitting(keysBox, keysL, keysI, keysPoint);

            for (; points > 0; points--)
                SpawnFitting(keysPoint, keysPoint, keysPoint, keysPoint);

            for (; lforms > 0; lforms--)
                SpawnFitting(keysL, keysI, keysPoint, keysPoint);

            for (; iforms > 0; iforms--)
                SpawnFitting(keysI, keysL, keysPoint, keysPoint);

            _waiting = true;
            await UniTask.WaitUntil(() =>
            {
                return _awaits.Count == 0 || _waiting == false;
            });
            if (_waiting) _awaits.Clear();
            _waiting = false;
        }

        private bool _waiting;
        private List<GameObject> spawned = new List<GameObject>();
        public void UndoWait()
        {
            if (_insts != null && _insts?.Count != 0)
            {
                _insts.ForEach(i => Destroy(i));
                _insts.Clear();
            }
            _waiting = false;
            spawned.ForEach(async g =>
            {
                await GetComponent<CanvasGroup>().DOFade(0f, 0.4f).SetEase(Ease.InOutSine).AsyncWaitForCompletion();
                Destroy(g);
            });
        }

        public bool TryGetCellPosition(Vector2 screenPos, Camera cam, out Vector2Int cell) =>
            blockblast.TryGetCellPosition(screenPos, cam, out cell);
        public bool TryPlace(Vector2Int origin, Vector2Int[] cells) => blockblast.TryPlace(origin, cells);
        public bool CanPlace(Vector2Int origin, Vector2Int[] cells) => blockblast.CanPlace(origin, cells);
        public Vector3 GetWorldFromCell(Vector2Int cell) => blockblast.GetWorldFromCell(cell);
        public BlockblastKey GetPointKey(Vector2Int cell) => blockblast.GetPointKey(cell);

        private void SpawnFitting(List<BlockblastKey> primary, List<BlockblastKey> fallback1, List<BlockblastKey> fallback2, List<BlockblastKey> fallback3)
        {
            var key = PickFitting(primary) ?? PickFitting(fallback1) ?? PickFitting(fallback2) ?? PickFitting(fallback3);
            if (key == null) return;
            Spawn(key);
        }

        private BlockblastKey PickFitting(List<BlockblastKey> keys)
        {
            if (keys == null || keys.Count == 0) return null;

            var shuffled = keys.OrderBy(_ => Random.value).ToList();
            foreach (var key in shuffled)
            {
                if (blockblast.HasFit(key.GetCells()))
                    return key;
            }

            return null;
        }

        private void Spawn(BlockblastKey template)
        {
            var inst = Instantiate(template.gameObject, template.transform.position, Quaternion.identity, template.transform.parent);
            inst.SetActive(true);
            _insts.Add(inst);
            spawned.Add(inst);

            AwaitCatch(inst.GetComponent<BlockblastKey>().Fill());
        }

        private static void CalcUniqueForms(int area, out int boxes, out int lforms, out int iforms, out int points)
        {
            boxes = 0;
            lforms = 0;
            iforms = 0;
            points = 0;
            
            switch (area)
            {
                case 1:
                    points = 1;
                    break;
                case 2:
                    iforms = 1;
                    break;
                case 3:
                    lforms = 1;
                    break;
                case 4:
                    boxes = 1;
                    break;
                case 5:
                    lforms = 1;
                    iforms = 1;
                    break;
                case 6:
                    boxes = 1;
                    iforms = 1;
                    break;
                case 7:
                    boxes = 1;
                    lforms = 1;
                    break;
                case 8:
                    boxes = 2;
                    break;
                case 9:
                    boxes = 1;
                    lforms = 1;
                    iforms = 1;
                    break;
                case 10:
                    boxes = 2;
                    iforms = 1;
                    break;
                case 11:
                    boxes = 2;
                    lforms = 1;
                    break;
                case 12:
                    boxes = 3;
                    break;
                default:
                    throw new Exception("Unknown area: " + area);
            }
        }
    }
}