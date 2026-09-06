using _Project.Scripts.Interactive;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.EntryPoints
{
    public class BootstrapEntryPoint : MonoBehaviour
    {
        private void Start()
        {
            InterItem.Stayed = null;
            InterItem.Stayed2 = null;
            DontDestroyOnLoad(this);
            SceneManager.LoadScene(1);
        }
    }
}