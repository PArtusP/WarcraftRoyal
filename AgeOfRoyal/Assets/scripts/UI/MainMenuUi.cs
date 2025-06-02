using System;
using System.Linq;
using Unity.Multiplayer.Playmode;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUi : MonoBehaviour
{
    [Header("Ui - Opening menu")]
    [SerializeField] GameObject OpeningMenu;

    [Header("Ui - Main menu")]
    [SerializeField] GameObject MainMenu;
    [SerializeField] Button PlayMenuButton;
    [SerializeField] Button SettingsButton;
    [SerializeField] Button ExitGameButton;

    [Header("Ui - Play menu")]
    [SerializeField] GameObject PlayMenu;
    [SerializeField] Button QuickGameButton;

    [Header("Ui - Play menu - Quick game menu")]
    [SerializeField] GameObject QuickGameMenu;
    [SerializeField] CharacterSelectorUi selector;
    [SerializeField] Button QuickMenu_Play;

    [Header("Ui - Settings menu")]
    [SerializeField] GameObject SettingsMenu;
    [SerializeField] Button Settings_Back;


    void Start()
    {
        DontDestroyOnLoad(gameObject);
        var mppmTag = CurrentPlayer.ReadOnlyTags();

#if SERVER_BUILD || HOST_BUILD 
        Destroy(GetComponent<NetworkManager>());
        Debug.Log("MainMenuUi, Start : Server load scene: GameScene");
        SceneManager.sceneLoaded += ServerLoadGameScene;
        SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
#else
    #if UNITY_EDITOR 
        if (mppmTag.Contains("Server") || mppmTag.Contains("Host"))
        {
            Destroy(GetComponent<NetworkManager>());
            SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
            SceneManager.sceneLoaded += ServerLoadGameScene; 
        } else
    #endif 
        BehaveAsClient(); 
#endif
    }

    public void BehaveAsClient()
    {
        MoveToOpeningMenu();
        // Main menu
        PlayMenuButton.onClick.AddListener(MoveToPlayMenu);
        SettingsButton.onClick.AddListener(MoveToSettingsMenu);
        ExitGameButton.onClick.AddListener(Application.Quit);
        // Play menu
        QuickGameButton.onClick.AddListener(MoveToQuickMenu);
        // Settings menu
        Settings_Back.onClick.AddListener(Back);

        OnlineInputManager.Controls.Menu.Back.performed += _ => Back();
    }

    private void ServerLoadGameScene(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= ServerLoadGameScene;
        Debug.Log($"MainMenuUi, ServerLoadGameScene : Server scene loaded: {scene.name}");
        var co = FindFirstObjectByType<ConnectionManager>();
#if SERVER_BUILD
        if (co.startServerAuto) co.Server();
        //Debug.unityLogger.logEnabled = false;
#elif HOST_BUILD
        if (co.startHostAuto) co.Host();
        Debug.unityLogger.logEnabled = false;
#elif UNITY_EDITOR
        Debug.unityLogger.logEnabled = true;
        var mppmTag = CurrentPlayer.ReadOnlyTags();
        if ((mppmTag.Contains("Server")) && co.startServerAuto) co.Server();
        if ((mppmTag.Contains("Host")) && co.startHostAuto) co.Host();
#endif
    }

    private void Back()
    {
        if (OpeningMenu.activeSelf)
            Application.Quit();
        else if (MainMenu.activeSelf)
            MoveToOpeningMenu();
        else if (PlayMenu.activeSelf)
            MoveToMainMenu();
        else if (QuickGameMenu.activeSelf)
            MoveToPlayMenu();
        else if (SettingsMenu.activeSelf)
            MoveToMainMenu();
    }

    void MoveToOpeningMenu()
    {
        MainMenu.SetActive(false);
        PlayMenu.SetActive(false);
        QuickGameMenu.SetActive(false);
        selector.gameObject.SetActive(false);
        SettingsMenu.SetActive(false);

        OpeningMenu.SetActive(true);
    }
    void MoveToMainMenu()
    {
        OpeningMenu.SetActive(false);
        PlayMenu.SetActive(false);
        QuickGameMenu.SetActive(false);
        selector.gameObject.SetActive(false);
        SettingsMenu.SetActive(false);

        MainMenu.SetActive(true);
    }
    void MoveToPlayMenu()
    {
        OpeningMenu.SetActive(false);
        MainMenu.SetActive(false);
        QuickGameMenu.SetActive(false);
        selector.gameObject.SetActive(false);
        SettingsMenu.SetActive(false);

        PlayMenu.SetActive(true);
    }
    void MoveToSettingsMenu()
    {
        OpeningMenu.SetActive(false);
        MainMenu.SetActive(false);
        QuickGameMenu.SetActive(false);
        selector.gameObject.SetActive(false);
        PlayMenu.SetActive(false);

        SettingsMenu.SetActive(true);
    }
    void MoveToQuickMenu()
    {
        OpeningMenu.SetActive(false);
        PlayMenu.SetActive(false);
        MainMenu.SetActive(false);
        SettingsMenu.SetActive(false);

        QuickGameMenu.SetActive(true);
        selector.gameObject.SetActive(true);

    }

    private void Update()
    {
        if (OpeningMenu.activeSelf == true && ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.allControls.Any(x => x is ButtonControl button && x.IsPressed() && !x.synthetic))))
        {
            MoveToMainMenu();
        }
    }

}
