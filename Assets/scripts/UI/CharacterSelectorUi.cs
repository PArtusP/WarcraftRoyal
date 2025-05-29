using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectorUi : MonoBehaviour
{

    [SerializeField] List<GameObject> prefabs = new List<GameObject>();
    [SerializeField] Transform gridUi;
    [SerializeField] GameObject buttonPrefab;
    [SerializeField] Image selectedImage;

    public List<GameObject> Prefabs { get => prefabs; set => prefabs = value; }

    //public GameObject CurrentCharacter { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        prefabs.ForEach(p =>
        {
            var obj = Instantiate<GameObject>(buttonPrefab, gridUi);
            var btn = obj.GetComponent<CharacterSelectorButton>();
            btn.Prefab = p;
            var prefab = btn.Prefab;
            var id = prefabs.FindIndex(0 , prefabs.Count, o => o == p);
            btn.Button.onClick.AddListener(delegate
            {
                if (PlayerSettings.CharacterId != id)
                {
                    PlayerSettings.CharacterId = id;
                    var player = prefab.GetComponent<Player>();
                    selectedImage.sprite = player.selectedSprite;
                }
            });
        });

    }
}
