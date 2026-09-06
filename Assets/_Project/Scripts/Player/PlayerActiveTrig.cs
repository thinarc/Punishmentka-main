using UnityEngine;

namespace _Project.Scripts.Player
{
    public class PlayerActiveTrig : MonoBehaviour
    {
        public static PlayerActiveTrig instance { get; private set; }
        
        public SpriteRenderer sprite;
        
        public LayerMask defMask;
        public string sortDefLayer;
        
        private LayerMask _defMask;
        private string _sortDefLayer;
        private int _sortOrder;

        private void Start()
        {
            instance = this;
            sprite = GetComponent(typeof(SpriteRenderer)) as SpriteRenderer;

            _defMask = gameObject.layer;
            _sortDefLayer = sprite.sortingLayerName;
            _sortOrder = sprite.sortingOrder;
        }
        
        public void Activ(int order)
        {
            gameObject.layer = defMask.value;
            sprite.sortingLayerName = sortDefLayer;
            sprite.sortingOrder = order;

            var pos = transform.position;
            pos.z = 0;
            transform.position = pos;
        }

        public void DeActiv()
        {
            gameObject.layer = _defMask;
            sprite.sortingLayerName = _sortDefLayer;
            sprite.sortingOrder = _sortOrder;
            
            var pos = transform.position;
            pos.z = -16;
            transform.position = pos;
        }
    }
}