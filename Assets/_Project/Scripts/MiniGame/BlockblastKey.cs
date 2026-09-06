using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.MiniGame
{
    public class BlockblastKey : MonoBehaviour
    {
        [SerializeField] private BlockForm blockForm = BlockForm.Box;
        private Vector2Int[] _cells;
        
        public BlockForm BlockForm => blockForm;
        public Vector2Int[] Cells => _cells;

        [Header("Debug")]
        [SerializeField, Space(5)] private List<Image> squares;

        private bool gridElement;
        private Animator anim;

        public void SetSprite(Sprite image)
        {
            GetComponentInChildren<Image>().sprite = image;
            gridElement = true;
            anim ??= GetComponent<Animator>();
            anim.SetBool("Drag", true);
            anim.SetBool("Catch", false);
            GetComponent<CanvasGroup>().DOFade(0, 0.4f).SetEase(Ease.InOutSine);
        }

        public void Show()
        {
            if (!gridElement) return;
            anim ??= GetComponent<Animator>();
            anim.SetBool("Catch", true);
            GetComponent<CanvasGroup>().DOFade(1, 0.4f).SetEase(Ease.InOutSine);
        }

        public Vector2Int[] GetCells()
        {
            _cells = CalcForm(blockForm);
            return _cells;
        }

        public async UniTask Fill(Sprite[] sheet)
        {
            var cells = GetComponentsInChildren<RectTransform>()[1];
            if (cells.name != "Cells") throw new Exception("Cells not found");
            var index = 0;
            
            squares = cells.GetComponentsInChildren<Image>().ToList();
            squares.ForEach(s =>
            {
                s.sprite = sheet[index];
                index++;
            });
            
            anim ??= GetComponent<Animator>();
            if (anim == null) return;
            await UniTask.WaitUntil(() =>
            {
                if (anim == null) return true;
                return anim.GetBool("Catch");
            });
        }

        private static Vector2Int[] CalcForm(BlockForm form)
        {
            return form switch
            {
                // (0,0) (1,0)
                // (0,1) (1,1)
                BlockForm.Box => new Vector2Int[] { new(0, 0), new(1, 0), new(0, 1), new(1, 1), },
                // (0,0) (1,0)
                BlockForm.IHorizontal => new Vector2Int[] { new(0, 0), new(1, 0), },
                // (0,0)
                // (0,1)
                BlockForm.IVertical => new Vector2Int[] { new(0, 0), new(0, 1), },
                // (0,0) (1,0)
                // (0,1)
                BlockForm.LUpLeft => new Vector2Int[] { new(0, 0), new(1, 0), new(0, 1), },
                // (0,0) (1,0)
                //       (1,1)
                BlockForm.LUpRight => new Vector2Int[] { new(0, 0), new(1, 0), new(1, 1), },
                //       (1,0)
                // (0,1) (1,1)
                BlockForm.LDownRight => new Vector2Int[] { new(1, 0), new(0, 1), new(1, 1), },
                // (0,0)
                // (0,1) (1,1)
                BlockForm.LDownLeft => new Vector2Int[] { new(0, 0), new(0, 1), new(1, 1), },
                BlockForm.Point => new Vector2Int[] { new(0, 0) },
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    
    public enum BlockForm
    {
        Box,
        LUpLeft,
        LUpRight,
        LDownRight,
        LDownLeft,
        IHorizontal,
        IVertical,
        Point
    }
}