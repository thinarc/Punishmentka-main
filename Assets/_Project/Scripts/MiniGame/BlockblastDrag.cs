using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Project.Scripts.MiniGame
{
    public class BlockblastDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BlockblastTools _tools;
        private BlockblastKey _key;
        private RectTransform _rect;
        private Canvas _canvas;
        private Animator _anim;
        
        private Vector3 _startPos;
        
        private bool _catched;

        private void Start()
        {
            _tools = GetComponentInParent<BlockblastTools>();
            _key = GetComponent<BlockblastKey>();
            _rect = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _anim = GetComponent<Animator>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_catched) return;
            _startPos = _rect.position;
            _anim.SetBool("Drag", true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_catched) return;
            _rect.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_catched) return;
            var cells = GetComponentInChildren<Cells>().GetComponent<RectTransform>();
            
            if (!_tools.TryGetCellPosition(eventData.position, eventData.pressEventCamera, out var cell))
            {
                _rect.DOMove(_startPos, 0.2f).SetEase(Ease.InOutSine);
                _anim.SetBool("Drag", false);
                return;
            }

            if (!_tools.TryPlace(cell, _key.Cells))
            {
                _rect.DOMove(_startPos, 0.24f).SetEase(Ease.InOutSine);
                _anim.SetBool("Drag", false);
                return;
            }
            
            cells.anchoredPosition = new Vector2(0, cells.anchoredPosition.y);
            var worldPos = _tools.GetWorldFromCell(cell);
            _rect.position = worldPos;
            _anim.SetBool("Catch", true);
            _catched = true;
        }
    }
}