using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NavMeshPlus.Extensions;
using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace _Project.Scripts.EntryPoints
{
    [ExecuteInEditMode]
    public class GameEntryPoint : MonoBehaviour
    {
        [SerializeField] private List<SceneState> states;
        
        [SerializeField, Space(5)] private Light2D[] globals;
        private float _targetIntensity;

        [SerializeField, Space(5)] private VolumeProfile[] profiles;
        [SerializeField] private Volume[] volumeCamera;

        private const string pixelCameraName = "Pixel Camera";

        [SerializeField, Space(5)] private MeshRenderer worldView;
        [SerializeField] private MeshRenderer worldViewRep;
        [SerializeField] private Material[] renderMaterials;
        [SerializeField] private Transform worldViewData;
        [SerializeField] private Transform worldViewSmallData;

        [SerializeField, Space(5)] private SimpleConfiner2D confiner;
        
        [SerializeField, Space(5)] private GameObject menuInterface;
        [SerializeField] private bool skipMenu;

        private int _lastOnStart = -100;

        private void Start()
        {
            _targetIntensity = globals[0].intensity;
            
            if (!Application.isPlaying) return;
            ChangeScene(true);
            states.ForEach(s =>
            {
                if (s.onStart) SoundManager.instance.PlayClip(s.sound);
            });
            
            // await UniTask.Delay(2000);
            // DoChangeScene(1);
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            ChangeLight(_targetIntensity);
        }
        
        public async void ChangeLight(float intensity)
        {
            _targetIntensity = intensity;
            if (Mathf.Approximately(globals[0].intensity, intensity)) return;
            globals[0].intensity = Mathf.MoveTowards(globals[0].intensity, intensity, Time.deltaTime / 40f);
            globals[1].intensity = Mathf.MoveTowards(globals[1].intensity, intensity, Time.deltaTime / 40f);
            await UniTask.Delay(3200);
            volumeCamera[0].profile = profiles[2];
        }
        
        public void ChangeLightM(float intensity)
        {
            _targetIntensity = intensity;
            if (Mathf.Approximately(globals[0].intensity, intensity)) return;
            globals[0].intensity = Mathf.MoveTowards(globals[0].intensity, intensity, Time.deltaTime / 35f);
            globals[1].intensity = Mathf.MoveTowards(globals[1].intensity, intensity, Time.deltaTime / 35f);
        }
        
        public void ChangeLightC(float intensity)
        {
            _targetIntensity = intensity;
            if (Mathf.Approximately(globals[0].intensity, intensity)) return;
            globals[0].intensity = Mathf.MoveTowards(globals[0].intensity, intensity, Time.deltaTime * 16f);
            globals[1].intensity = Mathf.MoveTowards(globals[1].intensity, intensity, Time.deltaTime * 16f);
        }
        
        public async void DoChangeScene(int index)
        {
            if (index < 0 || index >= states.Count) return;
            if (states[index].onStart) return;
            if (!Application.isPlaying) return;
            
            await WorldChangerEffect.instance.StartMenu(index);
            
            states.ForEach(s => s.onStart = false);
            states[index].onStart = true;
            ChangeScene(out var render, out var meshes);
            SoundManager.instance.PlayClip(states[index].sound);

            await WorldChangerEffect.instance.DoUndoMenu(render, meshes, index);
        }
        
        public RenderTexture normalView;
        public RenderTexture smallView;
        public Material worldViewM;
        public Material worldViewRepM;
        public Material worldViewSmallM;
        public Material worldViewSmallRepM;

        private async void ChangeScene(bool auto)
        {
            if (auto)
            {
                ChangeScene(out var render, out var viewsNoUsed);
                
                worldViewM.SetTexture("_MainTex", normalView);
                worldViewRepM.SetTexture("_MainTex", normalView);
                worldViewSmallM.SetTexture("_MainTex", smallView);
                worldViewSmallRepM.SetTexture("_MainTex", smallView);

                if (!Application.isPlaying)
                {
                    viewsNoUsed[0].sharedMaterial = render[0];
                    viewsNoUsed[1].sharedMaterial = render[1];
                    return;
                }

                var wait = UniTask.NextFrame();
                states.ForEach(s =>
                {
                    if (!skipMenu && s.onStart && s.scene.name == "Home") wait = MenuInterface.instance.StartMenu();
                });
                await wait;
                viewsNoUsed[0].sharedMaterial = render[0];
                viewsNoUsed[1].sharedMaterial = render[1];
            }
        }
        
        public GameObject TriggerTranslCam;
        private void ChangeScene(out Material[] render, out MeshRenderer[] views)
        {
            var r = Array.Empty<Material>();
            var v = Array.Empty<MeshRenderer>();
            
            states.ForEach(s =>
            {
                s.scene.SetActive(false);
                if (!s.onStart) return;

                s.scene.SetActive(true);
                confiner.boundsCollider = s.scene.GetComponent<PolygonCollider2D>();
                var vol = s.scene.name switch
                {
                    "Home" => 0,
                    "Fantasy" => 1,
                    "Final" => 3,
                    _ => -100
                };
                var isChangedView = s.scene.name switch
                {
                    "Home" => false,
                    "Fantasy" => false,
                    "Final" => true,
                    _ => false
                };
                var changeView = !isChangedView && volumeCamera[0].name != pixelCameraName || isChangedView && volumeCamera[0].name == pixelCameraName;
                if (changeView) (volumeCamera[0], volumeCamera[1]) = (volumeCamera[1], volumeCamera[0]);
                if (!volumeCamera[0].gameObject.activeInHierarchy) volumeCamera[0].gameObject.SetActive(true);
                if (volumeCamera[1].gameObject.activeInHierarchy) volumeCamera[1].gameObject.SetActive(false);
                volumeCamera[0].profile = profiles[vol];
                volumeCamera[1].profile = null;
                
                var renderMaterialsIndex = 0;
                if (isChangedView) renderMaterialsIndex = 2;
                r = new Material[2] { renderMaterials[renderMaterialsIndex], renderMaterials[renderMaterialsIndex + 1] };
                v = new MeshRenderer[2] { worldView,  worldViewRep };
                
                var scale = worldViewData.localScale;
                var pos = worldViewData.position;
                if (isChangedView) scale = worldViewSmallData.localScale;
                if (isChangedView) pos = worldViewSmallData.position;
                if (worldView.transform.localScale != scale) worldView.transform.localScale = scale;
                if (worldView.transform.position != pos) worldView.transform.position = pos;
                var lens = worldViewData.GetComponent<WorldViewLens>().lens;
                if (isChangedView) lens = worldViewSmallData.GetComponent<WorldViewLens>().lens;
                var allCam = confiner.GetComponent<Camera>();
                if (!Mathf.Approximately(allCam.orthographicSize, lens)) allCam.orthographicSize = lens;
                
                if (!skipMenu && s.scene.name == "Home") menuInterface.SetActive(true);
                else menuInterface.SetActive(false);
                TriggerTranslCam.SetActive(s.scene.name != "Final");
            });

            render = r;
            views = v;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            
            if (states.Count == 0 || states[0].scene == null)
            {
                states = new List<SceneState>();
                for (var i = 0; i < transform.childCount; i++) 
                    states.Add(new SceneState(transform.GetChild(i).gameObject, Resources.Load<AudioClip>(transform.GetChild(i).name)));
            }
            
            var allFalse = true;
            for (var i = 0; i < states.Count; i++)
            {
                if (!states[i].onStart) continue;
                allFalse = false;
                if (i == 0) _lastOnStart = 0;
                if (i == _lastOnStart || _lastOnStart == -100) continue;
                states[_lastOnStart].onStart = false;
                states[i].onStart = true;
                _lastOnStart = i;
            }
            if (allFalse)
            {
                states[0].onStart = true;
                _lastOnStart = 0;
            }
            
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) ChangeScene(true);
            };
        }
#endif
    }

    [System.Serializable]
    public class SceneState
    {
        [ReadOnly] public GameObject scene;
        [ReadOnly] public AudioClip sound;
        public bool onStart;

        public SceneState(GameObject scene, AudioClip sound)
        {
            this.scene = scene;
            this.sound = sound;
        }
    }
}
