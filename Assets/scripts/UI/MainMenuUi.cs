using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Multiplayer.Playmode;

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
        if (mppmTag.Contains("Server") || mppmTag.Contains("Host"))
        {
            var co = FindFirstObjectByType<ConnectionManager>();
            Destroy(GetComponent<NetworkManager>());
            SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
            if (mppmTag.Contains("Server") && co.startServerAuto) co.Server();
            if (mppmTag.Contains("Host") && co.startHostAuto) co.Host();
        }
        else
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
