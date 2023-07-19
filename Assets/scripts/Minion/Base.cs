using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Base : Hitable
{
    [SerializeField] GameObject minionPrefab;
    [SerializeField] Material material;

    // Start is called before the first frame update
    void Start()
    {
        home = this;
        StartCoroutine(SpawnMinion());
    }

    IEnumerator SpawnMinion()
    {
        var obj = Instantiate(minionPrefab, this.transform);
        obj.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial = material;
        obj.GetComponent<Hitable>().Home = this;
        //obj.GetComponent<Minion>().MeshRenderer.material = material;
        yield return new WaitForSeconds(5f);
        StartCoroutine(SpawnMinion());
    }
}
