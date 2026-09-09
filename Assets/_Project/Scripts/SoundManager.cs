using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Scripts
{
    public class SoundManager : MonoBehaviour
    {
        #region Singleton & Fields
        public static SoundManager instance;

        [Header("Audio Sources")]
        public AudioSource bg;
        public AudioSource mind;
        public AudioSource sfx;
        public AudioSource[] sfxR;

        [Header("State Variables")]
        public bool manual;
        public bool returned;
        public AudioClip pastClip;
        #endregion

        #region Lifecycle
        private void Awake() => instance = this;
        #endregion

        #region Global Controls
        public void PauseMusic()
        {
            bg.Pause();
            sfx.Pause();
        }

        public void ResumeMusic()
        {
            bg.Play();
            sfx.Play();
        }
        #endregion

        #region Sound Effects (SFX)
        public void PlaySfx(AudioClip clip, int vol)
        {
            if (vol == 6)
            {
                sfxR[vol].pitch = Random.Range(0.94f, 1.24f);
                sfxR[vol].panStereo = Random.Range(-0.8f, 0.8f);
                sfxR[vol].volume = Random.Range(0.024f, 0.04f);
            }
            sfxR[vol].PlayOneShot(clip);
        }

        public static void PSfx(AudioClip clip, int vol = 2) => instance.PlaySfx(clip, vol);

        public void PlayAfterPrepare(AudioClip clip)
        {
            sfx.clip = clip;
            sfx.loop = false;
            sfx.Play();
            sfx.DOFade(1, 1f).SetEase(Ease.Linear).SetUpdate(true);
        }
        #endregion

        #region Background Music (Standard)
        public async void PlayClip(AudioClip clip)
        {
            if (returned)
            {
                pastClip = clip;
                return;
            }
            
            var fade = 1.84f;
            if (bg.clip == null) fade = 0f;
            
            await bg.DOFade(0, fade).SetEase(Ease.Linear).SetUpdate(true).AsyncWaitForCompletion();
            bg.clip = clip;
            bg.Play();
            bg.DOFade(0.4f, 1.8f).SetEase(Ease.Linear).SetUpdate(true); // ИЗМЕНЕНО
        }

        public async void PlayClipManual(AudioClip clip)
        {
            manual = true;
            var fade = 2.7f;
            
            if (bg.clip == null)
            {
                fade = 0f;
                print("Fade out none??");
            }
            
            await bg.DOFade(0, fade).SetEase(Ease.Linear).SetUpdate(true).AsyncWaitForCompletion();
            bg.clip = clip;
            bg.loop = true;
            bg.Play();
            bg.DOFade(0.4f, 1.4f).SetEase(Ease.Linear).SetUpdate(true); // ИЗМЕНЕНО
        }

        public async void ReturnPlayClip(AudioClip clip)
        {
            var fade = 1.84f;
            if (bg.clip == null) fade = 0f;
            
            await bg.DOFade(0, fade).SetEase(Ease.Linear).SetUpdate(true).AsyncWaitForCompletion();
            
            pastClip = bg.clip;
            bg.clip = clip;
            bg.loop = false;
            bg.Play();
            bg.DOFade(0.4f, 2f).SetEase(Ease.Linear).SetUpdate(true); // ИЗМЕНЕНО
            returned = true;
            
            await UniTask.WaitWhile(() => bg.isPlaying);
            
            if (manual) return;
            
            bg.clip = pastClip;
            bg.loop = true;
            await bg.DOFade(0, 0.12f).SetEase(Ease.Linear).SetUpdate(true).AsyncWaitForCompletion();
            bg.Play();
            bg.DOFade(0.4f, 2.24f).SetEase(Ease.Linear).SetUpdate(true); // ИЗМЕНЕНО
            returned = false;
        }
        #endregion

        #region Mind / Ambient Music
        public async void PlayMind(AudioClip clip)
        {
            const float fade = 2f;
            
            bg.DOFade(0, fade).SetEase(Ease.Linear).SetUpdate(true).OnComplete(() =>
            {
                bg.Pause();
            });
            
            await UniTask.Delay(TimeSpan.FromSeconds(fade) / 2);
            
            if (mind.clip == null) mind.clip = clip;
            if (mind.loop == false) mind.loop = true;
            
            mind.DOFade(0, 0);
            mind.Play();
            mind.DOFade(0.3f, fade).SetEase(Ease.Linear).SetUpdate(true); // ИЗМЕНЕНО
        }
        
        public async void ReturnMind()
        {
            const float fade = 2f;
            
            mind.DOFade(0, fade).SetEase(Ease.Linear).SetUpdate(true).OnComplete(() =>
            {
                mind.Pause();
            });
            
            await UniTask.Delay(TimeSpan.FromSeconds(fade) / 2);
            
            bg.DOFade(0, 0);
            bg.Play();
            bg.DOFade(0.4f, fade).SetEase(Ease.Linear).SetUpdate(true); // ИЗМЕНЕНО
        }
        #endregion

        #region Special Fades (Cinematic / Focus)
        public async UniTask PrepareFade()
        {
            const float fade = 2.12f;
            await bg.DOFade(0.4f, fade).SetEase(Ease.Linear).SetUpdate(true).AsyncWaitForCompletion();
            await sfx.DOFade(0, 0.36f).SetEase(Ease.Linear).SetUpdate(true).AsyncWaitForCompletion();
        }
        
        public void ReturnFade()
        {
            const float fade = 2.12f;
            bg.DOFade(0.4f, fade).SetEase(Ease.Linear).SetUpdate(true); // ИЗМЕНЕНО
        }
        #endregion
    }
}