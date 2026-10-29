using System.Collections.Generic;
using UnityEngine;

namespace CostaBrava
{
    public class AudioManager : MonoBehaviour
    {
        public AudioClip[] footsteps;
        public AudioClip[] grassFootsteps;
        public AudioClip impactClip;
        public AudioClip explorationMusic, pursuitMusic;
        public AudioClip Engine { get; private set; }
        public AudioClip Siren { get; private set; }
        Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        AudioSource[] pool=new AudioSource[24];
        AudioSource calm,intense,ambient;
        int index;
        void Awake()
        {
            Engine=Tone("Engine",1,65,.5f,true);Siren=Tone("Siren",2,600,.65f,false);
            clips["shot"]=Tone("Shot",.15f,80,.6f,true);clips["reload"]=Tone("Reload",.2f,1300,.25f,true);
            clips["hit"]=Tone("Hit",.07f,1800,.2f,false);clips["hurt"]=Tone("Hurt",.2f,100,.3f,true);
            clips["reward"]=Tone("Reward",.4f,660,.4f,false);clips["alert"]=Tone("Alert",.6f,440,.4f,false);
            clips["jump"]=Tone("Jump",.15f,350,.15f,false);clips["skid"]=Tone("Skid",.3f,2200,.2f,true);clips["impact"]=impactClip;
            clips["explosion"]=Tone("Explosion",1.2f,45,.95f,true);clips["swing"]=Tone("Swing",.17f,220,.15f,true);
            for(int i=0;i<pool.Length;i++) {pool[i]=new GameObject("SFX pool "+i).AddComponent<AudioSource>();pool[i].transform.SetParent(transform);pool[i].spatialBlend=.8f;pool[i].minDistance=4;pool[i].maxDistance=65;}
            calm=Track(explorationMusic);intense=Track(pursuitMusic);
            ambient=gameObject.AddComponent<AudioSource>();ambient.clip=Tone("Coastal breeze",4,100,.05f,true);ambient.loop=true;ambient.volume=.1f;ambient.Play();
        }
        AudioSource Track(AudioClip clip) {var source=gameObject.AddComponent<AudioSource>();source.clip=clip;source.loop=true;source.volume=0;if(clip)source.Play();return source;}
        void Update()
        {
            if(!GameManager.Instance)return;
            bool pursuing=GameManager.Instance.wanted.Level>0;float level=GameManager.Instance.Started?.1f:.15f;
            calm.volume=Mathf.MoveTowards(calm.volume,pursuing?0:level,Time.unscaledDeltaTime*.08f);
            intense.volume=Mathf.MoveTowards(intense.volume,pursuing?.12f:0,Time.unscaledDeltaTime*.08f);
            AudioListener.volume=GameManager.Instance.Paused?.45f:1;
        }
        public void Play(string name,Vector3 position,float volume=1) {if(clips.TryGetValue(name,out var clip) && clip)PlayClip(clip,position,volume);}
        public void Footstep(Vector3 position)
        {
            var bank=footsteps;
            if(Physics.Raycast(position+Vector3.up*.2f,Vector3.down,out var hit,1,1) && (hit.collider.name.Contains("Parque") || hit.collider.name.Contains("Jardim")) && grassFootsteps!=null && grassFootsteps.Length>0)bank=grassFootsteps;
            if(bank!=null && bank.Length>0)PlayClip(bank[Random.Range(0,bank.Length)],position,.22f);
        }
        void PlayClip(AudioClip clip,Vector3 position,float volume) {var source=pool[index++%pool.Length];source.transform.position=position;source.pitch=Random.Range(.94f,1.06f);source.volume=volume;source.clip=clip;source.Play();}
        static AudioClip Tone(string name,float seconds,float hz,float amplitude,bool noisy)
        {
            const int rate=22050;int length=(int)(rate*seconds);var samples=new float[length];var random=new System.Random(73);
            for(int i=0;i<length;i++)
            {
                float t=(float)i/rate;float envelope=name=="Engine"||name=="Siren"||name=="Coastal breeze"?1:Mathf.Exp(-t*12);
                float frequency=name=="Siren"?hz+Mathf.Sin(t*Mathf.PI)*220:hz;
                float wave=Mathf.Sin(2*Mathf.PI*frequency*t);
                if(noisy)wave=wave*.4f+((float)random.NextDouble()*2-1)*.6f;
                samples[i]=wave*amplitude*envelope;
            }
            var clip=AudioClip.Create(name,length,1,rate,false);clip.SetData(samples,0);return clip;
        }
    }
}
