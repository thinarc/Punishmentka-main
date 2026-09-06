using Sirenix.Utilities;
using UnityEngine;
using System;
using System.Collections.Generic;
using _Project.Scripts.Player;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;

namespace _Project.Scripts.EntryPoints
{
    public class WorldChangerEffect : MonoBehaviour
    {
        public static WorldChangerEffect instance;
        private void Awake() => instance = this;
        
        public MeshRenderer texture;
        public Material blur;
        public GameObject[] upLayersFromHome;
        public GameObject[] upLayersToGarage;
        public GameObject[] upLayersToStreet;
        
        [ReadOnly, Space(5)] public List<GameObject> upLayersToHide;
        [ReadOnly] public List<GameObject> upLayersToShow;
        
        private float inkSpread = 15f;
        private bool inkSpeadAnim;
        
        public RenderTexture normalView;
        public RenderTexture smallView;
        
        private async UniTask BeforeMenu(bool toStreet = false)
        {
            texture.sharedMaterial.SetTexture("_MainTex", normalView);
            upLayersToHide.ForEach(g =>
            {
                if (g == null) throw new Exception("Null in upLayersToHide");
                if (g.TryGetComponent<PlayerMovement>(out var player)) player.disable = true;
                if (g.name == "New3" && g.transform.parent.TryGetComponent<Animator>(out var anim))
                    anim.enabled = false;
                g.GetComponentInChildren<SpriteRenderer>().DOFade(0, 0.44f).SetEase(Ease.InOutSine)
                    .OnComplete(() => g.SetActive(false));
            });
            upLayersToShow.ForEach(g =>
            {
                if (g == null) throw new Exception("Null in upLayersToShow");
                if (g.TryGetComponent<PlayerMovement>(out var player)) player.disable = true;
                if (g.TryGetComponent<Animator>(out var anim)) anim.enabled = false;
                g.GetComponentInChildren<SpriteRenderer>().DOFade(0, 0).OnComplete(() => g.SetActive(false));
            });

            // await UniTask.Delay(120);
            await ChangeEffect(1);
            
            if (toStreet)
            {
                texture.sharedMaterial.SetTexture("_MainTex", smallView);
            }
        }

        public async UniTask StartMenu(int numberUsage)
        {
            upLayersToHide ??= new List<GameObject>();
            upLayersToShow ??= new List<GameObject>();
            
            upLayersToHide.Clear();
            upLayersToShow.Clear();
            switch (numberUsage)
            {
                case 1:
                    upLayersFromHome.ForEach(l => upLayersToHide.Add(l));
                    upLayersToGarage.ForEach(l => upLayersToShow.Add(l));
                    break;
                case 2:
                    upLayersToGarage.ForEach(l => upLayersToHide.Add(l));
                    upLayersToStreet.ForEach(l => upLayersToShow.Add(l));
                    break;
                case 3:
                    upLayersToStreet.ForEach(l => upLayersToHide.Add(l));
                    break;
            }
            
            texture.material = blur;
            texture.sharedMaterial.SetFloat("_InkSpreadDistance", -5f);
            texture.sharedMaterial.SetFloat("_FadingFade", 0);
            texture.sharedMaterial.SetFloat("_GaussianBlurFade", 0);
            if (numberUsage == 2) await BeforeMenu(true);
            else await BeforeMenu();
        }

        public async UniTask DoUndoMenu(Material[] render, MeshRenderer[] meshes, int index)
        {
            await UndoMenu(render, meshes, index);
        }

        private async UniTask ChangeEffect(float target)
        {
            var speed = 1f;
            var speedS = 1.001f;
            if (target >= 1f)
            {
                speed = 0.7f;
                speedS = 1.00084f;
            }
            
            await UniTask.WaitWhile(() =>
            {
                var val = texture.sharedMaterial.GetFloat("_InkSpreadDistance");
                var t = !inkSpeadAnim ? 15f : -5f;
                var pastT = inkSpeadAnim ? -5f : 15f;
                val = Mathf.Lerp(pastT, t, speedS - Mathf.Exp(-0.2f * Time.unscaledDeltaTime));
                texture.sharedMaterial.SetFloat("_InkSpreadDistance", val);
                return !Mathf.Approximately(val, t);
            });
            
            await UniTask.Delay(600);
            
            await UniTask.WaitWhile(() =>
            {
                var val = texture.sharedMaterial.GetFloat("_FadingFade");
                val = Mathf.MoveTowards(val, target, speed * Time.deltaTime);
                texture.sharedMaterial.SetFloat("_FadingFade", val);
                return !Mathf.Approximately(val, target);
            });
            await UniTask.WaitWhile(() =>
            {
                var val = texture.sharedMaterial.GetFloat("_GaussianBlurFade");
                val = Mathf.MoveTowards(val, target, speed * Time.deltaTime);
                texture.sharedMaterial.SetFloat("_GaussianBlurFade", val);
                return !Mathf.Approximately(val, target);
            });
        }
        
        private async UniTask UndoMenu(Material[] newMats, MeshRenderer[] appliedMeshes, int index)
        {
            await UniTask.Delay(400);
            inkSpeadAnim = true;
            await ChangeEffect(0);

            texture = appliedMeshes[0];
            texture.sharedMaterial = newMats[0];
            appliedMeshes[1].sharedMaterial = newMats[1];
            if (index == 2)
            {
                texture.sharedMaterial.SetTexture("_MainTex", smallView);
                appliedMeshes[1].sharedMaterial.SetTexture("_MainTex", smallView);
            }
            else
            {
                texture.sharedMaterial.SetTexture("_MainTex", normalView);
                appliedMeshes[1].sharedMaterial.SetTexture("_MainTex", normalView);
            }
            var player = upLayersToShow[0];
            upLayersToShow[0] = null;
            upLayersToShow.ForEach(g =>
            {
                if (g == null) return;
                var sprite = g.GetComponentInChildren<SpriteRenderer>();
                sprite.DOFade(0, 0);
                g.SetActive(true);
                if (g.TryGetComponent<Animator>(out var anim)) sprite.DOFade(1, 0.4f).SetEase(Ease.InOutSine).OnComplete(() => anim.enabled = true);
                else sprite.DOFade(1, 0.4f).SetEase(Ease.InOutSine);
            });

            await UniTask.Delay(500);

            var sprite = player.GetComponentInChildren<SpriteRenderer>();
            sprite.DOFade(0, 0);
            player.SetActive(true);
            player.GetComponent<PlayerMovement>().disable = true;
            sprite.DOFade(1, 0.34f).SetEase(Ease.InOutSine);

            await UniTask.Delay(500);
            player.GetComponent<PlayerMovement>().disable = false;
        }
    }
}