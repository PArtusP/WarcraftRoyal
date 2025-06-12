
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;


static public class DbResolver
{
    static List<Minion> minions = null; 
    static List<UnitUpgrade> upgrades = null; 
    static List<UnitModule> modules = null; 
    static List<UnitAction> actions = null; 
    public static List<Minion> Minions
    {
        get
        {
            if (minions == null)
                minions = Resources.LoadAll<Minion>("").ToList();
            return minions;
        }
    } 
    public static List<UnitUpgrade> Upgrades
    {
        get
        {
            if (upgrades == null)
                upgrades = Resources.LoadAll<UnitUpgrade>("").ToList();
            return upgrades;
        }
    } 
    public static List<UnitModule> Modules
    {
        get
        {
            if (modules == null)
                modules = Resources.LoadAll<UnitModule>("").ToList();
            return modules;
        }
    } 
    public static List<UnitAction> Actions
    {
        get
        {
            if (actions == null)
                actions = Resources.LoadAll<UnitAction>("").ToList();
            return actions;
        }
    } 

    public static Minion GetMinionById(int id)
        => Minions.FirstOrDefault(m => m.ID == id) ?? throw new Exception($"Minion with ID {id} not found in resources.");
    public static UnitUpgrade GetUpgradeById(int id)
        => Upgrades.FirstOrDefault(m => m.ID == id) ?? throw new Exception($"Upgrade with ID {id} not found in resources.");
    public static UnitModule GetModuleById(int id)
        => Modules.FirstOrDefault(m => m.ID == id) ?? throw new Exception($"Module with ID {id} not found in resources.");
    public static UnitAction GetActionById(int id)
        => Actions.FirstOrDefault(m => m.ID == id) ?? throw new Exception($"Action with ID {id} not found in resources.");
}



public static class Utils
{
    public static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
    {
        while (toCheck != null && toCheck != typeof(object))
        {
            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
            if (generic == cur)
            {
                return true;
            }
            toCheck = toCheck.BaseType;
        }
        return false;
    }
    public static string TPM_MarkupBullets(this string rawText, char findBullet = '•', float indent = 0.5f, float leftMargin = 0f)
    {
        bool isInBullet = false;
        string markedup = String.Format("<margin-left={0}em>", leftMargin);
        string indentLeftTag = String.Format("<indent={0}em>", indent), indentRightTag = "</indent>";
        foreach (char c in rawText)
        {
            if (c == findBullet) { markedup += findBullet + indentLeftTag; isInBullet = true; }
            else if (isInBullet && c == (char)10) { markedup += indentRightTag + c; isInBullet = false; }
            else { markedup += c; }
        }
        if (isInBullet) markedup += indentRightTag;
        markedup += "</margin>";
        return markedup;
    }
}
public static class GameMaths
{
    public static Vector3 GetVectorFromAngle(float angle)
    {
        // angle = 0 -> 360
        float angleRad = angle * (Mathf.PI / 180f);
        return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }
}

static public class GameSVfx
{
    static List<AudioClip> getHurtClips = null;
    static List<AudioClip> dieClips = null;

    public static List<AudioClip> GetHurtClips
    {
        get
        {
            if (getHurtClips == null)
                getHurtClips = Resources.LoadAll("GetHurt", typeof(AudioClip)).Cast<AudioClip>().ToList();
            return getHurtClips;
        }
    }
    public static List<AudioClip> DieClips
    {
        get
        {
            if (dieClips == null)
                dieClips = Resources.LoadAll("Die", typeof(AudioClip)).Cast<AudioClip>().ToList();
            return dieClips;
        }
    }
    public class Volumes
    {
        internal static float getHit = .4f;
        internal static float mapObjectBreak = .3f;
        internal static float weaponTriggered = .3f;
        internal static float gunReload = .2f;
        internal static float evadeSuccess = .25f;

        internal static float getHurt = .2f;
        internal static float die = .5f;
    }

    static public void PlayRandom(List<AudioClip> list, float volume, MonoBehaviour sourceObj)
    {
        var source = sourceObj.gameObject.AddComponent<AudioSource>();
        source.volume = Volumes.getHurt;
        source.clip = list[UnityEngine.Random.Range(0, list.Count)];
        source.Play();
        sourceObj.StartCoroutine(WaitToDestroy(source));
    }
    static public AudioSource PlaySoundOneShot(AudioClip clip, float volume, MonoBehaviour sourceObj)
    {
        var source = sourceObj.gameObject.AddComponent<AudioSource>();
        source.volume = volume;
        source.clip = clip;
        source.Play();
        sourceObj.StartCoroutine(WaitToDestroy(source));
        return source;
    }
    static public void PlaySoundOneShot(List<AudioClip> clips, float volume, MonoBehaviour sourceObj)
    {
        clips.ForEach(c =>
        {
            var source = sourceObj.gameObject.AddComponent<AudioSource>();
            source.volume = volume;
            source.clip = c;
            source.Play();
            sourceObj.StartCoroutine(WaitToDestroy(source));
        });
    }
    static public void PlayEffectOneShot(List<ParticleSystem> particles, Quaternion rot, Vector3 pos, Transform parent, MonoBehaviour source)
    {
        var inst = particles.Select(p => UnityEngine.Object.Instantiate(p, pos + p.transform.position, rot, parent)).ToList();
        inst.ForEach(p => { p.Play(); source.StartCoroutine(WaitToDestroy(p)); });
    }
    static public void PlayEffectOneShot(List<VisualEffect> effects, Quaternion rot, Vector3 pos, Transform parent, MonoBehaviour source)
    {
        var inst = effects.Select(p => UnityEngine.Object.Instantiate(p, pos + p.transform.position, rot, parent)).ToList();
        inst.ForEach(p =>
        {
            p.Play();
            source.StartCoroutine(WaitToDestroy(p));
        });
    }

    static IEnumerator WaitToDestroy(AudioSource source)
    {
        yield return new WaitForSeconds(source.clip.length);
        GameObject.Destroy(source);
    }
    static IEnumerator WaitToDestroy(ParticleSystem particles)
    {
        yield return new WaitForSeconds(particles.main.duration);
        var em = particles.emission;
        em.enabled = false;
        yield return new WaitForSeconds(particles.main.startLifetime.constantMax);
        GameObject.Destroy(particles.gameObject);
    }
    static IEnumerator WaitToDestroy(VisualEffect effect)
    {
        var starTime = Time.time;
        yield return new WaitForSeconds(1f);
        while ((effect.HasAnySystemAwake() || effect.aliveParticleCount > 0) && Time.time - starTime > 20f)
            yield return new WaitForEndOfFrame();
        GameObject.Destroy(effect.gameObject);
    }
} 
static public class GameTools
{
    public static void SetGamePause(bool value)
    {
        if (value)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;
    }
}
static class GameColor
{
    public static Color SELF_COLOR => new Color(.3f, .2f, 1f);
    public static Color OPPS_COLOR => new Color(1f, .2f, .3f);
    public static Color BONUS => new Color(0f, .4f, .2f);
    public static Color MALUS => new Color(.6f, 0f, 0f);
}
static class GameValues
{
    public static float parriedTime => .5f;
    public static float knockOutTime => 2.7f;
}


class LayerData
{
    public LayerMask Mask { get => 1 << _value; }
    public int Value { get => _value; }

    int _value;

    public LayerData(int value)
    {
        _value = value;
    }
}

static class GameLayers
{
    public static LayerData Default = new LayerData(0);
    public static LayerData TransparentFX = new LayerData(1);
    public static LayerData IgnoreRaycast = new LayerData(2);
    public static LayerData Hitable = new LayerData(3);
    public static LayerData Water = new LayerData(4); 

    static public bool IsInLayer(int layer, LayerMask mask)
    {
        return mask == (mask | (1 << layer));
    }
}

public class HierarchyHelper : MonoBehaviour
{
    // Method to get hierarchy path from parent to child
    public static string GetHierarchyPath(Transform parent, Transform child)
    {
        if (parent == null || child == null)
        {
            throw new System.ArgumentNullException("Neither parent nor child transform can be null.");
        }

        // Start with the parent's name
        string path = "";

        // Check if the child is part of the parent's hierarchy
        Transform current = child;
        while (current != null)
        {
            if (current == parent)
            {
                return path;  // Return the path if child is found in the parent's hierarchy
            }

            // Prepend parent name to path
            path = current.name + "/" + path;
            current = current.parent;
        }

        // If we exit the loop, the child was not in the parent's hierarchy
        throw new System.Exception("Child transform is not in the parent's hierarchy.");
    }
}

static class Geometry
{
    public static Vector3 GetVectorFromAngle(float angle)
    {
        // angle = 0 -> 360
        float angleRad = angle * (Mathf.PI / 180f);
        return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }
    public static Vector3 FlatVector(Vector3 vect) => new Vector3(vect.x, 0, vect.z);
}
