using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        private Vector2Int _grabOffset;

        private bool _catched;
        private readonly List<BlockblastKey> _previewCells = new();

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
            _grabOffset = GetGrabOffset(eventData);
            _anim.SetBool("Drag", true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_catched) return;
            _rect.anchoredPosition += eventData.delta / _canvas.scaleFactor;
            UpdatePreview(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_catched) return;
            var cells = GetComponentInChildren<Cells>().GetComponent<RectTransform>();

            ClearPreviewCells();

            if (!_tools.TryGetCellPosition(eventData.position, eventData.pressEventCamera, out var pointerCell))
            {
                ResetToStart(0.2f);
                return;
            }

            var origin = pointerCell - _grabOffset;

            if (!_tools.TryPlace(origin, _key.Cells))
            {
                ResetToStart(0.24f);
                return;
            }

            cells.anchoredPosition = new Vector2(0, cells.anchoredPosition.y);
            var worldPos = _tools.GetWorldFromCell(origin);
            _rect.position = worldPos;
            _anim.SetBool("Catch", true);
            _catched = true;

            var group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.DOFade(0, 0.3f).SetEase(Ease.InOutSine)
                .OnComplete(() => Destroy(gameObject));
        }

        private void UpdatePreview(PointerEventData eventData)
        {
            ClearPreviewCells();

            if (!_tools.TryGetCellPosition(eventData.position, eventData.pressEventCamera, out var pointerCell))
                return;

            var origin = pointerCell - _grabOffset;
            var valid = _tools.CanPlace(origin, _key.Cells);

            foreach (var cellOffset in _key.Cells)
            {
                var pointKey = _tools.GetPointKey(origin + cellOffset);
                if (pointKey == null) continue;
                pointKey.Preview(valid);
                _previewCells.Add(pointKey);
            }
        }

        private void ClearPreviewCells()
        {
            foreach (var cell in _previewCells)
            {
                if (cell != null) cell.ClearPreview();
            }
            _previewCells.Clear();
        }

        private void ResetToStart(float duration)
        {
            ClearPreviewCells();
            _rect.DOMove(_startPos, duration).SetEase(Ease.InOutSine);
            _anim.SetBool("Drag", false);
        }

        private Vector2Int GetGrabOffset(PointerEventData eventData)
        {
            var shape = _key.Cells;
            if (shape == null || shape.Length == 0)
                return Vector2Int.zero;

            // The shape's own per-cell images ("Key1".."Key4") are the only reliable
            // geometry: the root rect is a fixed 20x22 tray-slot box shared by every
            // shape, so mapping the pointer against it (instead of the actual cell
            // images) put the grab point in the wrong place for anything but Box.
            // Image order here matches shape order 1:1 - the same assumption Fill() relies on.
            var cellImages = GetComponentInChildren<Cells>().GetComponentsInChildren<Image>();

            var cam = eventData.pressEventCamera;
            for (var i = 0; i < shape.Length && i < cellImages.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(cellImages[i].rectTransform, eventData.position, cam))
                    return shape[i];
            }

            // Pointer landed between/outside the cell images (e.g. grabbed the slack
            // margin around the shape) - fall back to whichever cell center is closest.
            var best = shape[0];
            var bestDist = float.MaxValue;
            for (var i = 0; i < shape.Length && i < cellImages.Length; i++)
            {
                var screenPos = RectTransformUtility.WorldToScreenPoint(cam, cellImages[i].rectTransform.position);
                var dist = (screenPos - eventData.position).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = shape[i];
                }
            }

            return best;
        }
    }
}