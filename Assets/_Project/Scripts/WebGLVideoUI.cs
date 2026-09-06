using System;
using _Project.Scripts.EntryPoints;
using _Project.Scripts.Pincode;
using _Project.Scripts.Player;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.Video;
using Random = UnityEngine.Random;

namespace _Project.Scripts
{
    public class WebGLVideoUI : MonoBehaviour
    {
        public CanvasGroup leavetext;
        public VideoPlayer player;
        public AudioClip clip;
        public RawImage raw;

        public Light2D l1;
        public Light2D l2;
        public bool preparedddd = false;
        public bool preparedddd2 = false;

        public Light2D l4;
        public Light2D l5;
        public Light2D l44;
        public Light2D l55;

        private bool clipshot;

        private async void Update()
        {
            if (preparedddd)
            {
                l1.intensity = Mathf.MoveTowards(l1.intensity, 1.84f, Time.deltaTime * 0.4f);
                l2.intensity = Mathf.MoveTowards(l1.intensity, 1.84f, Time.deltaTime * 0.4f);
                if (clipshot) return;
                clipshot = true;
                SoundManager.PSfx(Resources.Load<AudioClip>("UsableVFx/cig/Trimmed_поджег"));
                await UniTask.Delay(1340);
                SoundManager.PSfx(Resources.Load<AudioClip>("UsableVFx/cig/Trimmed_дыхание"));
            }

            if (preparedddd2)
            {
                l4.intensity = Mathf.MoveTowards(l4.intensity, 0f, Time.deltaTime * 0.8f);
                l5.intensity = Mathf.MoveTowards(l5.intensity, 0f, Time.deltaTime * 0.4f);
                l44.intensity = Mathf.MoveTowards(l44.intensity, 0f, Time.deltaTime * 0.8f);
                l55.intensity = Mathf.MoveTowards(l55.intensity, 0f, Time.deltaTime * 0.4f);
            }
        }

        private async void Start()
        {
            raw.material.SetFloat("_FadingFade", 0);

            // await UniTask.Delay(500);
            // PlayVideo();
        }

        public async void PlayVideo()
        {
            FindAnyObjectByType<GameEntryPoint>().ChangeLightC(0f);
            
            var pl = FindAnyObjectByType<PlayerMovement>();
            pl.disable = true;

            await UniTask.Delay(1000);
            
            pl.SetTarget(new Vector2(-0.40f, -0.67f));
            await UniTask.Delay(2740);
            pl.SetTarget(new Vector2(-0.67f, -0.57f));

            await UniTask.Delay(6000);
            l4.GetComponentInParent<Animator>().enabled = false;
            l5.GetComponentInParent<Animator>().enabled = false;
            preparedddd2 = true;
            
            await UniTask.Delay(3600);
            
            preparedddd = true;
            FindAnyObjectByType<PlayerMovement>().GetComponentInChildren<SpriteRenderer>().DOFade(0, 0.2f).SetEase(Ease.OutBack);
            
            await UniTask.Delay(3600);
            await UniTask.Delay(2400);
            await UniTask.Delay(200);
            await FindAnyObjectByType<CalledPin>().CheckPin();
            
            var filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "shotvideo.mp4");
            player.url = filePath;
            
            player.Prepare();
            player.prepareCompleted += OnPrepared;
        }

        private async void OnPrepared(VideoPlayer vp)
        {
            await SoundManager.instance.PrepareFade();
            var group = GetComponent<CanvasGroup>();
            group.blocksRaycasts = true;
            group.interactable = true;
            group.DOFade(1, 0.4f).SetEase(Ease.InOutSine);
            SoundManager.instance.PlayAfterPrepare(clip);
            vp.Play();

            player.loopPointReached += async _ =>
            {
                SoundManager.instance.ReturnFade();
                await UniTask.Delay(600);
                ShowBye();
                await UniTask.WaitWhile(() =>
                {
                    var val = Mathf.MoveTowards(raw.material.GetFloat("_FadingFade"), 1, Time.deltaTime * Random.Range(0.07f, 0.24f));
                    raw.material.SetFloat("_FadingFade", val);
                    return !Mathf.Approximately(val, 1);
                });

                async void ShowBye()
                {
                    await UniTask.Delay(940);
                    leavetext.alpha = 0;
                    leavetext.gameObject.SetActive(true);
                    leavetext.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).From(0.82f);
                    leavetext.DOFade(1, 0.4f).SetEase(Ease.OutBack);
                }
            };
        }
    }
}