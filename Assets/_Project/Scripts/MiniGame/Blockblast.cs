using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Scripts.Interactive;
using _Project.Scripts.Player;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace _Project.Scripts.MiniGame
{
    public class Blockblast : ItemView
    {
        [SerializeField] private RectTransform source;
        [SerializeField] private RectTransform grid;

        [SerializeField, Space(5)] private BlockblastTools tools;
        
        private Animator _anim;

        [Header("Debug")]
        [SerializeField] private List<BlockblastKey> points;
        private bool[,] _shape = new bool[8, 8];

        public InterActivator[] flowers;
        public InterActivator teddy;
        public InterActivator door;

        public bool inGame;

        private void Start()
        {
            _anim = GetComponent<Animator>();
            // RunGame();
        }
        
        public InterItem item;
        private int cyclegame = 1; // next cycle
        public async override void SeeView()
        {
            if (inGame) return;
            inGame = true;
            if (specialDesk)
            {
                var player = FindAnyObjectByType<PlayerMovement>();
                player.disable = true;
                player.SetTarget(new Vector2(0.41f, -0.56f));
                await UniTask.Delay(2000);
                player.SetTarget(new Vector2(0.14f, -0.52f));
                await UniTask.Delay(200);
            }
            
            wait = true;
            SoundManager.instance.PlayMind(Resources.Load<AudioClip>("Mindgame"));
            cyclegame++;
            await RunCycle(cyclegame - 1);
            SoundManager.instance.ReturnMind();
            UnseeView();
        }
        
        public void ReInvoke()
        {
            // if (cyclegame is 2 or 4) item.oneshot = false;
            // else item.oneshot = true;
            item.oneshot = false;
            item.NowOneshot();
        }

        public override void UnseeView()
        {
            var player = FindAnyObjectByType<PlayerMovement>();
            player.disable = false;
            
            wait = false;
            if (cyclegame is 2) flowers[0].enabled = true;
            else if (cyclegame is 4)
            {
                teddy.enabled = true;
                flowers[1].enabled = true;
            }
            else if (cyclegame is 5)
            {
                door.enabled = true;
            }

            inGame = false;
        }

        // public async void RunGame()
        // {
        //     await RunCycle(1);
        //     await RunCycle(2);
        //     await RunCycle(3);
        //     await RunCycle(4);
        // }

        private Sprite[] GetPhoto(int i, bool getForSource = false)
        {
            var index = i;
            if (getForSource) index = Convert.ToInt32(i + "1");
            return Resources.LoadAll<Sprite>($"Grids/{index}");
        }

        private async UniTask RunCycle(int photo)
        {
            _anim.ResetTrigger("Show");
            _anim.ResetTrigger("Hide");
            _finishFill = false;
            _hideEnd = false;
            progress.value = 0;
            targetProgress = 0;
            _startValue = 0;
            _elapsed = 0;
            RollProgressThresholds();
            _cycle = 0;
            tools.ResetKeys();
            _shape = new bool[8, 8];
            grid.GetComponent<CanvasGroup>().DOFade(1, 0);
            
            _anim.enabled = true;
            _anim.SetTrigger("Show");

            await ShowProgress(true);
            
            var sprites = GetPhoto(photo);
            source.GetComponent<Image>().sprite = GetPhoto(photo, true)[0];
            source.GetComponent<Image>().DOFade(0, 0);
            points = grid.GetComponentsInChildren<BlockblastKey>().ToList();
            var i = 0;
            points.ForEach(p =>
            {
                p.SetSprite(sprites[i]);
                i++;
            });
            
            while (true)
            {
                if (!_finishFill) await ShowKeys();
                else
                {
                    // await ShowProgress();
                    await grid.GetComponent<CanvasGroup>().DOFade(0, 0.24f).SetEase(Ease.InOutSine).AsyncWaitForCompletion();
                    await UniTask.Delay(400);
                    await source.GetComponent<Image>().DOFade(1, 0.44f).SetEase(Ease.InOutSine).AsyncWaitForCompletion();
                    _anim.ResetTrigger("Show");
                    _anim.SetTrigger("Hide");
                    _anim.enabled = true;
                    _anim.ResetTrigger("Show");
                    _anim.SetTrigger("Hide");
                    _hideEnd = false;
                    await UniTask.WaitUntil(() => _hideEnd);
                    break;
                }
                await ShowProgress();
            }
        }

        private bool _hideEnd;

        [SerializeField] private Slider progress;

        
        private float _startValue;
        private float _elapsed;
        [SerializeField] private float duration = 0.5f;
        private void Update()
        {
            if (_elapsed >= duration) return;

            _elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(_elapsed / duration);

            // InOutSine
            float eased = -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;

            progress.value = Mathf.Lerp(_startValue, targetProgress, eased);
        }
        private float targetProgress;

        [SerializeField] private int minShowsPerCycle = 3;
        [SerializeField] private int maxShowsPerCycle = 5;
        private readonly List<float> _progressThresholds = new();
        private int _nextThreshold;

        // Picks how many times (minShowsPerCycle..maxShowsPerCycle, inclusive)
        // the progress bar will pop up over this whole picture cycle, then lays
        // out that many random, increasing progress thresholds to pop at. The
        // last threshold is left implicit - the finishing 100% (_finishFill) is
        // always shown regardless, so only showCount-1 thresholds are rolled here.
        private void RollProgressThresholds()
        {
            _progressThresholds.Clear();
            _nextThreshold = 0;

            var showCount = Random.Range(minShowsPerCycle, maxShowsPerCycle + 1);
            var nonFinalShows = showCount - 1;
            if (nonFinalShows <= 0) return;

            var segment = 1f / showCount;
            for (var i = 1; i <= nonFinalShows; i++)
            {
                var jitter = Random.Range(-0.4f, 0.4f) * segment;
                _progressThresholds.Add(Mathf.Clamp01(i * segment + jitter));
            }
            _progressThresholds.Sort();
        }

        public async UniTask ShowProgress(bool start = false)
        {
            if (start)
            {
                progress.gameObject.SetActive(false);
                return;
            }

            // By the time the final batch finishes, ClearCompleted() has already
            // reset _shape back to empty to start the reveal - so GetProgress()
            // would read back 0 here. The board being fully filled is exactly
            // what _finishFill means, so the target is trivially 100%.
            var currentProgress = _finishFill ? 1f : GetProgress();

            // Only pop the bar when a rolled threshold is actually crossed (so it
            // shows ~minShowsPerCycle..maxShowsPerCycle times per cycle instead of
            // on every batch), plus always on the finishing 100%. A jump that
            // crosses several thresholds at once still only shows once.
            if (!_finishFill)
            {
                if (_nextThreshold >= _progressThresholds.Count || currentProgress < _progressThresholds[_nextThreshold])
                    return;

                while (_nextThreshold < _progressThresholds.Count && currentProgress >= _progressThresholds[_nextThreshold])
                    _nextThreshold++;
            }

            progress.GetComponent<CanvasGroup>().DOFade(0, 0);
            progress.gameObject.SetActive(true);
            await progress.GetComponent<CanvasGroup>().DOFade(1f, 0.4f).SetEase(Ease.InOutSine).AsyncWaitForCompletion();
            _startValue = progress.value;
            targetProgress = currentProgress;
            _elapsed = 0f;

            await UniTask.WaitUntil(() => Mathf.Approximately(progress.value, targetProgress));
            await UniTask.Delay(200);
            await progress.GetComponent<CanvasGroup>().DOFade(0f, 0.4f).SetEase(Ease.InOutSine).AsyncWaitForCompletion();
            progress.gameObject.SetActive(false);
        }
        
        public float GetProgress()
        {
            int filled = 0;
            int width = _shape.GetLength(0);
            int height = _shape.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (_shape[x, y])
                        filled++;
                }
            }

            int total = width * height;

            return (float)filled / total;
        }

        private int _cycle;
        public async UniTask ShowKeys()
        {
            _cycle++;
            var effectiveCycle = _cycle + Mathf.RoundToInt(GetProgress() * 6f);
            var randSquare = Random.Range(5, 13);
            if (effectiveCycle > 3 && effectiveCycle < 4) randSquare = Random.Range(4, 11);
            if (effectiveCycle > 4) randSquare = Random.Range(3, 10);
            var wait = tools.InitKeys(randSquare);
            await tools.GetComponent<CanvasGroup>().DOFade(0, 0).AsyncWaitForCompletion();
            tools.GetComponent<CanvasGroup>().DOFade(1f, 0.4f).SetEase(Ease.InOutSine);
            await wait;
        }

        public bool TryGetCellPosition(Vector2 screenPos, Camera cam, out Vector2Int cell)
        {
            cell = default;

            if (points == null || points.Count != 8 * 8)
                return false;

            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (point == null) continue;

                var pointRect = point.GetComponent<RectTransform>();
                if (pointRect == null) continue;

                if (!RectTransformUtility.RectangleContainsScreenPoint(pointRect, screenPos, cam))
                    continue;

                cell = new Vector2Int(i % 8, i / 8);
                return true;
            }

            return false;
        }

        public bool CanPlace(Vector2Int origin, Vector2Int[] cells)
        {
            if (cells == null || cells.Length == 0)
                return false;

            foreach (var cell in cells)
            {
                var target = origin + cell;

                if (target.x < 0 || target.y < 0 || target.x >= 8 || target.y >= 8)
                    return false;

                if (_shape[target.x, target.y])
                    return false;
            }

            return true;
        }

        public bool HasFit(Vector2Int[] cells)
        {
            if (cells == null || cells.Length == 0)
                return false;

            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    if (CanPlace(new Vector2Int(x, y), cells))
                        return true;
                }
            }

            return false;
        }

        // Snapshot of the occupied grid, used to simulate a whole batch of
        // not-yet-spawned blocks before committing to it (see BlockblastTools).
        public bool[,] CloneShape()
        {
            var clone = new bool[8, 8];
            Array.Copy(_shape, clone, _shape.Length);
            return clone;
        }

        public static bool CanPlaceOn(bool[,] grid, Vector2Int origin, Vector2Int[] cells)
        {
            foreach (var cell in cells)
            {
                var target = origin + cell;

                if (target.x < 0 || target.y < 0 || target.x >= 8 || target.y >= 8)
                    return false;

                if (grid[target.x, target.y])
                    return false;
            }

            return true;
        }

        public bool TryPlace(Vector2Int origin, Vector2Int[] cells)
        {
            if (!CanPlace(origin, cells))
                return false;

            var targets = new Vector2Int[cells.Length];
            for (var i = 0; i < cells.Length; i++)
                targets[i] = origin + cells[i];

            foreach (var target in targets)
            {
                _shape[target.x, target.y] = true;
                points[target.x + target.y * 8].ShowBlock();
            }

            bool allFilled = true;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (!_shape[x, y])
                        allFilled = false;
                }
            }

            if (allFilled)
            {
                _finishFill = true;
                tools.UndoWait();
            }

            ClearCompleted();

            return true;
        }

        private void ClearCompleted()
        {
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    if (!_shape[x, y])
                        return;
                }
            }
            
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    _shape[x, y] = false;

                    var index = x + y * 8;
                    points[index].RevealPhoto();
                }
            }
        }

        private bool _finishFill;

        public Vector3 GetWorldFromCell(Vector2Int cell)
        {
            if (cell.x < 0 || cell.y < 0 || cell.x >= 8 || cell.y >= 8 ||
                points == null || points.Count != 8 * 8)
                return grid.position;

            return points[cell.x + cell.y * 8].transform.position;
        }

        public BlockblastKey GetPointKey(Vector2Int cell)
        {
            if (cell.x < 0 || cell.y < 0 || cell.x >= 8 || cell.y >= 8 ||
                points == null || points.Count != 8 * 8)
                return null;

            return points[cell.x + cell.y * 8];
        }

        private void OnDrawGizmosSelected()
        {
            if (grid == null) return;
            var layout = grid.GetComponent<GridLayoutGroup>();
            if (layout == null) return;

            var rect = grid.rect;
            var pitch = layout.cellSize + layout.spacing;
            var topLeftX = rect.xMin + layout.padding.left;
            var topLeftY = rect.yMax - layout.padding.top;

            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    Gizmos.color = _shape[x, y] ? Color.red : Color.green;

                    var local = new Vector3(
                        topLeftX + x * pitch.x + layout.cellSize.x / 2f,
                        topLeftY - y * pitch.y - layout.cellSize.y / 2f);

                    var pos = grid.TransformPoint(local);

                    Gizmos.DrawCube(pos, new Vector3(layout.cellSize.x, layout.cellSize.y, 0.01f));
                }
            }
        }

        public void BlockAnim()
        {
            _anim.enabled = false;
            _hideEnd = true;
        }
    }
}
