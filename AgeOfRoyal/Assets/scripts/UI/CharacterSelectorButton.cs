using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectorButton : MonoBehaviour
{
    [SerializeField] new TMPro.TextMeshProUGUI name;
    [SerializeField] Image image;
    [SerializeField] GameObject prefab;
    [SerializeField] Image selectedImage;
    private Button button;

    public string Name { get => name.text; set => name.text = value; }
    public Sprite Image { get => image.sprite; set => image.sprite = value; }
    public Button Button { get => button; set => button = value; }
    public Sprite SelectedImage { get => selectedImage.sprite; set => selectedImage.sprite = value; }

    public GameObject Prefab { get => prefab; set 
        {
            prefab = value;
            var player = prefab.GetComponent<Player>();
            SelectedImage = player.selectedSprite;
            button = gameObject.GetComponent<Button>();

            if (player.iconSprite != null)
                Image = player.iconSprite;
            else
                Name = prefab.name;
        } 
    }
}
