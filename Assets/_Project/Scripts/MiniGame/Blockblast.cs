using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Scripts.Interactive;
using _Project.Scripts.Player;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace _Project.Scripts.MiniGame
{
    public class Blockblast : ItemView
    {
        [SerializeField] private RectTransform source;
        [SerializeField] private RectTransform grid;
        [SerializeField, Space(5)] private float cellSize;
        
        [SerializeField, Space(5)] private BlockblastTools tools;
        
        private Animator _anim;

        [Header("Debug")]
        [SerializeField, Space(5)] private List<Sprite> sheet;
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
            sheet.Clear();
            _finishFill = false;
            _hideEnd = false;
            progress.value = 0;
            targetProgress = 0;
            _startValue = 0;
            _elapsed = 0;
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
            sprites.ForEach(s => sheet.Add(s));
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

        public async UniTask ShowProgress(bool start = false)
        {
            if (start) 
            {
                progress.gameObject.SetActive(false);
                return;
            }

            progress.GetComponent<CanvasGroup>().DOFade(0, 0);
            progress.gameObject.SetActive(true);
            await progress.GetComponent<CanvasGroup>().DOFade(1f, 0.4f).SetEase(Ease.InOutSine).AsyncWaitForCompletion();
            _startValue = progress.value;
            targetProgress = GetProgress();
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
            var randSquare = Random.Range(5, 13);
            if (_cycle > 3 && _cycle < 4) randSquare = Random.Range(4, 11);
            if (_cycle > 4) randSquare = Random.Range(3, 10);
            var wait = tools.InitKeys(sheet, randSquare);
            await tools.GetComponent<CanvasGroup>().DOFade(0, 0).AsyncWaitForCompletion();
            tools.GetComponent<CanvasGroup>().DOFade(1f, 0.4f).SetEase(Ease.InOutSine);
            await wait;
        }

        public bool TryGetCellPosition(Vector2 screenPos, Camera cam, out Vector2Int cell)
        {
            cell = default;
            
            screenPos.y += 92f;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(source, screenPos, cam, out var local)) return false;
            local.y *= -1;
            
            local.x += source.rect.width / 2;
            local.y += source.rect.height;

            var x = Mathf.FloorToInt(local.x * cellSize) - 1;
            var y = Mathf.FloorToInt(local.y * cellSize) - 1;

            if (x < 0 || y < 0 || x >= 8 || y >= 8)
                return false;

            cell = new Vector2Int(x, y);
            return true;
        }
        
        public bool TryPlace(Vector2Int origin, Vector2Int[] cells)
        {
            foreach (var c in cells)
            {
                var x = origin.x + c.x;
                if (x is < 0 or >= 8) x = origin.x - c.x;
                var y = origin.y + c.y;
                if (y is < 0 or >= 8) y = origin.y - c.y;
                
                if (x < 0 || y < 0 || x >= 8 || y >= 8)
                    return false;
                
                // if (_shape[x, y])
                //     return false;
            }

            foreach (var c in cells)
            {
                var x = origin.x + c.x;
                if (x is < 0 or >= 8) x = origin.x - c.x;
                var y = origin.y + c.y;
                if (y is < 0 or >= 8) y = origin.y - c.y;
                _shape[x, y] = true;
                points[x + y * 8].Show();
            }

            List<int> free = new();

            // собрать все свободные клетки
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (!_shape[x, y])
                        free.Add(x + y * 8);
                }
            }

            // перемешать
            for (int i = 0; i < free.Count; i++)
            {
                int rnd = Random.Range(i, free.Count);
                (free[i], free[rnd]) = (free[rnd], free[i]);
            }

            // взять первые addes
            int count = Mathf.Min(Random.Range(4, 8), free.Count);
            var predict = free.Count - count;
            if (predict > 0 && predict < 7)
                count = free.Count;

            for (int i = 0; i < count; i++)
            {
                int index = free[i];
                int x = index % 8;
                int y = index / 8;

                _shape[x, y] = true;
                points[index].Show();
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
            
            return true;
        }

        private bool _finishFill;
        
        public Vector3 GetWorldFromCell(Vector2Int cell)
        {
            var x = cell.x / cellSize;
            var y = 56 - cell.y / cellSize;

            var local = new Vector2(x, y);
            
            local.x -= source.rect.width * source.pivot.x;
            local.y -= source.rect.height * source.pivot.y;
            if (local.x < 0) local.x += 8;
            if (local.y < 0) local.y += 8;

            return source.TransformPoint(local);
        }
        
        private void OnDrawGizmosSelected()
        {
            var topLeft = new Vector3(source.rect.xMin, source.rect.yMax);

            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    Gizmos.color = _shape[x, y] ? Color.red : Color.green;

                    var pos = topLeft;
                    pos += new Vector3(cellSize * 32, cellSize * -32);
                    pos += new Vector3(x * cellSize * 64, y * cellSize * -64);
                    pos = source.TransformPoint(pos);

                    Gizmos.DrawCube(pos, new Vector3(cellSize, cellSize));
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
