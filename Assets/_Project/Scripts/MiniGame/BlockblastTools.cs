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
        
        public async UniTask InitKeys(List<Sprite> sheet, int area)
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
            {
                var randBox = keysBox[Random.Range(0, keysBox.Count)];
                
                if (TryGetAnchor(randBox, out var ax, out var ay))
                {
                    var instBox =Instantiate(randBox.gameObject, new Vector3(randBox.transform.position.x, randBox.transform.position.y, randBox.transform.position.z), 
                        Quaternion.identity, randBox.transform.parent);
                    instBox.SetActive(true);
                    _insts.Add(instBox);
                    spawned.Add(instBox);
                    
                    var localSheet = BuildSheetForKey(instBox.GetComponent<BlockblastKey>(), sheet, ax, ay);
                    AwaitCatch(instBox.GetComponent<BlockblastKey>().Fill(localSheet));
                }
                else return;
            }
            
            for (; points > 0; points--)
            {
                var point = keysPoint[0];
                
                if (TryGetAnchor(point, out var ax, out var ay))
                {
                    var instBox =Instantiate(point.gameObject, new Vector3(point.transform.position.x, point.transform.position.y, point.transform.position.z), 
                        Quaternion.identity, point.transform.parent);
                    instBox.SetActive(true);
                    _insts.Add(instBox);
                    spawned.Add(instBox);
                    
                    var localSheet = BuildSheetForKey(instBox.GetComponent<BlockblastKey>(), sheet, ax, ay);
                    AwaitCatch(instBox.GetComponent<BlockblastKey>().Fill(localSheet));
                }
                else return;
            }
            
            for (; lforms > 0; lforms--)
            {
                var randL = keysL[Random.Range(0, keysL.Count)];
                
                if (TryGetAnchor(randL, out var ax, out var ay))
                {
                    var instBox =Instantiate(randL.gameObject, new Vector3(randL.transform.position.x, randL.transform.position.y, randL.transform.position.z), 
                        Quaternion.identity, randL.transform.parent);
                    instBox.SetActive(true);
                    _insts.Add(instBox);
                    spawned.Add(instBox);
                    
                    var localSheet = BuildSheetForKey(instBox.GetComponent<BlockblastKey>(), sheet, ax, ay);
                    AwaitCatch(instBox.GetComponent<BlockblastKey>().Fill(localSheet));
                }
                else return;
            }
            
            for (; iforms > 0; iforms--)
            {
                var randI = keysI[Random.Range(0, keysI.Count)];
                
                if (TryGetAnchor(randI, out var ax, out var ay))
                {
                    var instBox =Instantiate(randI.gameObject, new Vector3(randI.transform.position.x, randI.transform.position.y, randI.transform.position.z), 
                        Quaternion.identity, randI.transform.parent);
                    instBox.SetActive(true);
                    _insts.Add(instBox);
                    spawned.Add(instBox);
                    
                    var localSheet = BuildSheetForKey(instBox.GetComponent<BlockblastKey>(), sheet, ax, ay);
                    AwaitCatch(instBox.GetComponent<BlockblastKey>().Fill(localSheet));
                }
                else return;
            }

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
        public Vector3 GetWorldFromCell(Vector2Int cell) => blockblast.GetWorldFromCell(cell);
        
        private static Sprite CalcSprite(List<Sprite> sheet, int x, int y) => sheet[y * 8 + x];
        
        private static Sprite[] BuildSheetForKey(BlockblastKey key, List<Sprite> sheet, int anchorX, int anchorY)
        {
            var cells = key.GetCells();
            var result = new Sprite[cells.Length];

            for (var i = 0; i < cells.Length; i++)
            {
                var px = anchorX + cells[i].x;
                var py = anchorY + cells[i].y;

                result[i] = CalcSprite(sheet, px, py);
            }
            return result;
        }
        
        private static bool TryGetAnchor(BlockblastKey key, out int ax, out int ay)
        {
            var cells = key.GetCells();

            var maxX = 0;
            var maxY = 0;
            
            foreach (var c in cells)
            {
                if (c.x > maxX) maxX = c.x;
                if (c.y > maxY) maxY = c.y;
            }

            var limitX = 8 - (maxX + 1);
            var limitY = 8 - (maxY + 1);

            if (limitX < 0 || limitY < 0)
            {
                ax = ay = 0;
                return false;
            }

            ax = Random.Range(0, limitX + 1);
            ay = Random.Range(0, limitY + 1);
            return true;
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