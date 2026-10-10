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

            // Every pick below is validated against this one snapshot of the real
            // board, together with everything already committed this batch - a
            // piece only joins the batch if the result stays fully safe (see
            // IsFullySafe), i.e. placeable in full no matter what order or
            // position the player actually picks. If nothing keeps a slot safe,
            // it's dropped and the batch just ends up smaller.
            var baseline = blockblast.CloneShape();
            var committed = new List<BlockblastKey>();

            for (; boxes > 0; boxes--)
                TryCommitFitting(baseline, committed, keysBox, keysL, keysI, keysPoint);

            for (; points > 0; points--)
                TryCommitFitting(baseline, committed, keysPoint, keysPoint, keysPoint, keysPoint);

            for (; lforms > 0; lforms--)
                TryCommitFitting(baseline, committed, keysL, keysI, keysPoint, keysPoint);

            for (; iforms > 0; iforms--)
                TryCommitFitting(baseline, committed, keysI, keysL, keysPoint, keysPoint);

            committed.ForEach(Spawn);

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

        private void TryCommitFitting(bool[,] baseline, List<BlockblastKey> committed,
            List<BlockblastKey> primary, List<BlockblastKey> fallback1, List<BlockblastKey> fallback2, List<BlockblastKey> fallback3)
        {
            var key = PickCommittable(baseline, committed, primary)
                ?? PickCommittable(baseline, committed, fallback1)
                ?? PickCommittable(baseline, committed, fallback2)
                ?? PickCommittable(baseline, committed, fallback3);

            if (key != null) committed.Add(key);
        }

        // Tries each candidate (in random order) as an addition to what's already
        // committed this batch, and keeps the first one for which the whole set
        // stays safe regardless of the order/positions the player ends up
        // choosing (see IsFullySafe). If nothing keeps it safe, this slot is
        // simply left out - the batch shrinks instead of risking a stuck piece.
        private BlockblastKey PickCommittable(bool[,] baseline, List<BlockblastKey> committed, List<BlockblastKey> keys)
        {
            if (keys == null || keys.Count == 0) return null;

            var shapes = committed.Select(k => k.GetCells()).ToList();
            var shuffled = keys.OrderBy(_ => Random.value).ToList();
            foreach (var key in shuffled)
            {
                shapes.Add(key.GetCells());
                var grid = (bool[,])baseline.Clone();
                if (IsFullySafe(grid, shapes))
                    return key;
                shapes.RemoveAt(shapes.Count - 1);
            }

            return null;
        }

        // True only if, no matter which of these shapes the player places first,
        // and no matter which of its legal positions they choose, the rest can
        // still always be completed the same way, recursively. This is strictly
        // stronger than "some simultaneous layout exists" - it rules out the
        // player's own placement order ever stranding a piece mid-batch.
        // Cheap enough here because a batch is at most a handful of pieces.
        private static bool IsFullySafe(bool[,] grid, List<Vector2Int[]> shapes)
        {
            if (shapes.Count == 0) return true;

            // Most-constrained-first so a genuine dead end is found fast.
            var order = Enumerable.Range(0, shapes.Count)
                .OrderBy(i => CountCandidatePositions(grid, shapes[i]))
                .ToArray();

            foreach (var shapeIndex in order)
            {
                var shape = shapes[shapeIndex];
                var rest = new List<Vector2Int[]>(shapes.Count - 1);
                for (var i = 0; i < shapes.Count; i++)
                    if (i != shapeIndex) rest.Add(shapes[i]);

                var hasAnyPosition = false;
                for (var y = 0; y < 8; y++)
                {
                    for (var x = 0; x < 8; x++)
                    {
                        var origin = new Vector2Int(x, y);
                        if (!Blockblast.CanPlaceOn(grid, origin, shape)) continue;
                        hasAnyPosition = true;

                        foreach (var cell in shape) grid[origin.x + cell.x, origin.y + cell.y] = true;
                        var safe = IsFullySafe(grid, rest);
                        foreach (var cell in shape) grid[origin.x + cell.x, origin.y + cell.y] = false;

                        if (!safe) return false;
                    }
                }

                if (!hasAnyPosition) return false;
            }

            return true;
        }

        private static int CountCandidatePositions(bool[,] grid, Vector2Int[] shape)
        {
            var count = 0;
            for (var y = 0; y < 8; y++)
                for (var x = 0; x < 8; x++)
                    if (Blockblast.CanPlaceOn(grid, new Vector2Int(x, y), shape))
                        count++;
            return count;
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