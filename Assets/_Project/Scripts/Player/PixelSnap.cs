using UnityEngine;

namespace _Project.Scripts.Player
{
    [ExecuteInEditMode]
    public class PixelSnap : MonoBehaviour
    {
        private const int PixelsPerUnit = 32;

        private float _lastX;
        private float _lastY;
        
        private void Update()
        {
            _lastX = transform.position.x;
            _lastY = transform.position.y;
        }

        private void FixedUpdate()
        {
            if (Mathf.Approximately(gameObject.transform.position.x, _lastX) && Mathf.Approximately(gameObject.transform.position.y, _lastY)) return;

            gameObject.transform.position = new Vector3(((float)((int)(gameObject.transform.position.x * PixelsPerUnit)) / PixelsPerUnit),
                ((float)((int)(gameObject.transform.position.y * PixelsPerUnit)) / PixelsPerUnit),
                gameObject.transform.position.z);
            _lastX = gameObject.transform.position.x;
            _lastY = gameObject.transform.position.y;
        }

        public void Move(int pixelX, int pixelY)
        {
            gameObject.transform.position = new Vector3(((float)((int)(gameObject.transform.position.x * PixelsPerUnit) + pixelX) / PixelsPerUnit),
                ((float)((int)(gameObject.transform.position.y * PixelsPerUnit) + pixelY) / PixelsPerUnit),
                gameObject.transform.position.z);
            _lastX = gameObject.transform.position.x;
            _lastY = gameObject.transform.position.y;
        }
    }
}