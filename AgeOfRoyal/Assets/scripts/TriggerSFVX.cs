using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

[Serializable]
public class TriggerSVFX
{
    public List<TriggerVisualEffect> effects;
    public List<TriggerEffectGO> gameObjects;
    public List<TriggerParticleSystem> particles;
    public List<TriggerRenderer> renderers;
    public List<Animation> animations;
    public List<AudioClip> sounds;

    private List<AudioSource> sources = new List<AudioSource>();
    private bool playing;
    private Coroutine stopCoroutine;

    public bool StopSound { get; private set; } = false;

    public bool Playing => playing;

    internal void PlayBase(bool value, MonoBehaviour owner, bool stopSound = false, float? volume = null, float? time = null, Vector3? position = null, Quaternion? rotation = null)
    {
        playing = value;
        this.StopSound = stopSound;
        if (time.HasValue)
        {
            effects.ForEach(e => e.Timer = time.Value);
            gameObjects.ForEach(e => e.Timer = time.Value);
            particles.ForEach(e => e.Timer = time.Value);
            renderers.ForEach(e => e.Timer = time.Value);
        }
        if (value)
        {
            if (stopCoroutine != null)
            {
                owner.StopCoroutine(stopCoroutine);
                stopCoroutine = null;
            }
            sources.Clear();
            effects.ForEach(e => e.Play(owner, position, rotation));
            gameObjects.ForEach(e => e.Play(owner, position, rotation));
            particles.ForEach(e => e.Play(owner, position, rotation));
            renderers.ForEach(e => e.Play(owner, position, rotation));

            animations.ForEach(a => a.Play());
            sources.AddRange(sounds.Select(s => GameSVfx.PlaySoundOneShot(s, volume.HasValue ? volume.Value : GameSVfx.Volumes.weaponTriggered, owner)));

            var total = new List<TriggerEffectBase>();
            total.AddRange(effects);
            total.AddRange(gameObjects);
            total.AddRange(particles);
            total.AddRange(renderers);
            var maxTime = time ?? (total.Any() ? total.Max(e => e.Timer.HasValue ? e.Timer.Value : 0f) : 0f);
            if (maxTime != 0f) stopCoroutine = owner.StartCoroutine(Stop(maxTime));
        }
        else
            ClearEffect();
    }

    private void ClearEffect(bool forced = false)
    {
        renderers.Where(r => forced || !r.Timer.HasValue).ToList().ForEach(r => r.Stop());
        particles.Where(p => forced || !p.Timer.HasValue).ToList().ForEach(p => p.Stop());
        effects.Where(e => forced || !e.Timer.HasValue).ToList().ForEach(e => e.Stop());
        gameObjects.Where(e => forced || !e.Timer.HasValue).ToList().ForEach(e => e.Stop());
        if (StopSound) sources.ForEach(s => { if (s) s.Stop(); });
        sources.Clear();
    }

    IEnumerator Stop(float maxTime)
    {
        yield return new WaitForSeconds(maxTime);
        playing = false;
        ClearEffect(true);
    }
    public TriggerSVFX Clone()
    {
        return new TriggerSVFX
        {
            effects = effects?.Select(e => e?.Clone() as TriggerVisualEffect).ToList(),
            gameObjects = gameObjects?.Select(go => go?.Clone() as TriggerEffectGO).ToList(),
            particles = particles?.Select(p => p?.Clone() as TriggerParticleSystem).ToList(),
            renderers = renderers?.Select(r => r?.Clone() as TriggerRenderer).ToList(),
            animations = new List<Animation>(animations), // Shared reference unless custom logic is needed
            sounds = new List<AudioClip>(sounds),         // Shallow copy is fine for AudioClips
                                                          // sources and playing state are not copied (private/internal runtime state)
        };
    }
}
[Serializable]
abstract public class TriggerEffectBase
{
    [SerializeField] protected bool instanciate = false;
    [SerializeField] protected bool asChild = false;
    protected GameObject instance = null;

    [SerializeField] protected float timer = 0;
    protected MonoBehaviour owner = null;
    public float? Timer { get => timer == 0 ? null : timer; set => timer = value.Value; }
    public GameObject Instance { get => instance; }
}

[Serializable]
abstract public class TriggerEffect<T> : TriggerEffectBase
{
    public T effect; 
    protected T instanceEffect; 
    public T Effect { get => instanciate ? instanceEffect : effect; }

    virtual protected void Set(bool value, MonoBehaviour owner, Vector3? position = null, Quaternion? rotation = null)
    {
        this.owner = owner;
        if (value && Timer.HasValue)
            owner.StartCoroutine(WaitToStop());
        if (value && instanciate)
            InstanciateInternal(position, rotation, owner);

        SetInternal(value);
        if (!value && instanciate) UnityEngine.Object.Destroy(instance);
    }
    public void Play(MonoBehaviour owner, Vector3? position = null, Quaternion? rotation = null) => Set(true, owner, position, rotation);
    public void Stop() => Set(false, owner);
    protected IEnumerator WaitToStop()
    {
        yield return new WaitForSeconds(timer);
        Stop();
    }

    abstract protected void InstanciateInternal(Vector3? position = null, Quaternion? rotation = null, MonoBehaviour owner = null);
    abstract protected void SetInternal(bool value); 
    
    public virtual TriggerEffect<T> Clone()
    {
        var copy = (TriggerEffect<T>)MemberwiseClone();
        copy.instance = null;
        copy.instanceEffect = default;
        copy.owner = null;
        return copy;
    }
}
[Serializable]
public class TriggerEffectGO : TriggerEffect<GameObject>
{
    protected override void InstanciateInternal(Vector3? position = null, Quaternion? rotation = null, MonoBehaviour owner = null)
    {
        instanceEffect = UnityEngine.Object.Instantiate(effect,
            position.HasValue ? position.Value + effect.transform.position : owner.transform.position + effect.transform.position,
            rotation.HasValue ? rotation.Value * effect.transform.rotation : owner.transform.rotation * effect.transform.rotation, asChild ? owner.transform : null);
        instance = instanceEffect.gameObject;
    }

    protected override void SetInternal(bool value)
    {
        if (Effect)
            Effect.SetActive(value);
    }
    public override TriggerEffect<GameObject> Clone()
    {
        var copy = new TriggerEffectGO
        {
            effect = this.effect,
            instanciate = this.instanciate,
            asChild = this.asChild,
            Timer = this.Timer
        };
        return copy;
    }
}
[Serializable]
abstract public class TriggerEffectComp<T> : TriggerEffect<T> where T : Component
{

    override protected void InstanciateInternal(Vector3? position = null, Quaternion? rotation = null, MonoBehaviour owner = null)
    {
        instanceEffect = UnityEngine.Object.Instantiate(effect,
            position.HasValue ? position.Value + effect.transform.position : owner.transform.position + effect.transform.position,
            rotation.HasValue ? rotation.Value * effect.transform.rotation : owner.transform.rotation * effect.transform.rotation, asChild ? owner.transform : null);
        instance = instanceEffect.gameObject;
    }
}

[Serializable]
public class TriggerVisualEffect : TriggerEffectComp<VisualEffect>
{
    protected override void SetInternal(bool value)
    {
        if (!Effect) return;
        if (value) Effect.Play();
        else Effect.Stop();
    }
    public override TriggerEffect<VisualEffect> Clone()
    {
        var copy = new TriggerVisualEffect
        {
            effect = this.effect,
            instanciate = this.instanciate,
            asChild = this.asChild,
            Timer = this.Timer
        };
        return copy;
    }
}
[Serializable]
public class TriggerParticleSystem : TriggerEffectComp<ParticleSystem>
{
    protected override void SetInternal(bool value)
    {
        if (value)
        {
            if (Effect.isPaused || Effect.isStopped) Effect.Play();

            //var em = Effect.emission;
            //em.enabled = true;
        }
        else
        {
            if (Effect)
                Effect.Stop();
            //var em = Effect.emission;
            //em.enabled = false;
        }
    }
    public override TriggerEffect<ParticleSystem> Clone()
    {
        var copy = new TriggerParticleSystem
        {
            effect = this.effect,
            instanciate = this.instanciate,
            asChild = this.asChild,
            Timer = this.Timer
        };
        return copy;
    }
}
[Serializable]
public class TriggerRenderer : TriggerEffectComp<Renderer>
{
    protected override void SetInternal(bool value) { var r = instanciate ? instanceEffect : effect; r.enabled = value; }
    public override TriggerEffect<Renderer> Clone()
    {
        var copy = new TriggerRenderer
        {
            effect = this.effect,
            instanciate = this.instanciate,
            asChild = this.asChild,
            Timer = this.Timer
        };
        return copy;
    }
}
