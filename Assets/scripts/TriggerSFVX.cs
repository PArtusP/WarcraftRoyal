using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
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

    internal void PlayBase(bool value, MonoBehaviour owner, bool stopSound = false, float? volume = null, float? time = null, Vector3? position = null, Quaternion? rotation = null)
    {
        if (time.HasValue)
        {
            effects.ForEach(e => e.Timer = time.Value);
            gameObjects.ForEach(e => e.Timer = time.Value);
            particles.ForEach(e => e.Timer = time.Value);
            renderers.ForEach(e => e.Timer = time.Value);
        }
        if (value)
        {
            sources.Clear();
            effects.ForEach(e => e.Play(owner, position, rotation));
            gameObjects.ForEach(e => e.Play(owner, position, rotation));
            particles.ForEach(e => e.Play(owner, position, rotation));
            renderers.ForEach(e => e.Play(owner, position, rotation));
            animations.ForEach(a => a.Play());
            sources.AddRange(sounds.Select(s => GameSVfx.PlaySoundOneShot(s, volume.HasValue ? volume.Value : GameSVfx.Volumes.weaponTriggered, owner)));
        }
        else
        {
            renderers.Where(r => !r.Timer.HasValue).ToList().ForEach(r => r.Stop());
            particles.Where(p => !p.Timer.HasValue).ToList().ForEach(p => p.Stop());
            effects.Where(e => !e.Timer.HasValue).ToList().ForEach(e => e.Stop());
            gameObjects.Where(e => !e.Timer.HasValue).ToList().ForEach(e => e.Stop());
            if (stopSound) sources.ForEach(s => { if (s) s.Stop(); });
            sources.Clear();
        }
    }
}
[Serializable]
abstract public class TriggerEffect<T>
{
    public T effect;

    [SerializeField] protected bool instanciate = false;
    protected GameObject instance = null;
    protected T instanceEffect;

    [SerializeField] private float timer = 0;
    protected MonoBehaviour owner = null;

    public float? Timer { get => timer == 0 ? null : timer; set => timer = value.Value; }
    public T Effect { get => instanciate ? instanceEffect : effect; }
    public GameObject Instance { get => instance; }

    virtual protected void Set(bool value, MonoBehaviour owner, Vector3? position = null, Quaternion? rotation = null)
    {
        this.owner = owner;
        if (value && Timer.HasValue)
            owner.StartCoroutine(WaitToStop());
        if (value && instanciate)
            InstanciateInternal(position, rotation);

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

    abstract protected void InstanciateInternal(Vector3? position = null, Quaternion? rotation = null);
    abstract protected void SetInternal(bool value);
}
[Serializable]
public class TriggerEffectGO : TriggerEffect<GameObject>
{
    protected override void InstanciateInternal(Vector3? position = null, Quaternion? rotation = null)
    {
        instanceEffect = UnityEngine.Object.Instantiate(effect,
            position.HasValue ? position.Value + effect.transform.position : owner.transform.position + effect.transform.position,
            rotation.HasValue ? rotation.Value * effect.transform.rotation : owner.transform.rotation * effect.transform.rotation, null);
        instance = instanceEffect.gameObject;
    }

    protected override void SetInternal(bool value)
    {
        if (Effect)
            Effect.SetActive(value);
    }
}
[Serializable]
abstract public class TriggerEffectComp<T> : TriggerEffect<T> where T : Component
{

    override protected void InstanciateInternal(Vector3? position = null, Quaternion? rotation = null)
    {
        instanceEffect = UnityEngine.Object.Instantiate(effect,
        position.HasValue ? position.Value + effect.transform.position : owner.transform.position + effect.transform.position,
            rotation.HasValue ? rotation.Value * effect.transform.rotation : owner.transform.rotation * effect.transform.rotation, null);
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
}
[Serializable]
public class TriggerRenderer : TriggerEffectComp<Renderer>
{
    protected override void SetInternal(bool value) { var r = instanciate ? instanceEffect : effect; r.enabled = value; }
}
