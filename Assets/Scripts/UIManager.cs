using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject UICanvas;
    public Button[] CharSelectButtons;
    public Button StartButton;  
    public TMPro.TMP_InputField IdField;


    public GameObject GameEndPannel;
    
    private int _playerTemplateNum = 0;
    
    
    public static UIManager Instance { get; private set; }
    
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    
    void Start()
    {
        CharSelectButtons[0].onClick.AddListener(() => SelectPlayerTemplate(0));
        CharSelectButtons[1].onClick.AddListener(() => SelectPlayerTemplate(1));
        
        
        StartButton.onClick.AddListener(() =>
        {
            if (IdField.text == null)
                return;
            SuperManager.Instance.playerId = IdField.text;
            TcpProtobufClient.Instance.SendLoginMessage(IdField.text, _playerTemplateNum);
            UICanvas.SetActive(false);
        });
    }

    public void GameEnd()
    {
        UICanvas.SetActive(true);
        GameEndPannel.SetActive(true);
    }

    private void SelectPlayerTemplate(int playerTemplateNum)
    {
        _playerTemplateNum = playerTemplateNum;

        for (int i = 0; i < CharSelectButtons.Length; i++)
        {
            if (i == playerTemplateNum)
            {
                CharSelectButtons[i].image.color = Color.gray;
            }
            else
            {
                CharSelectButtons[i].image.color = Color.white;
            }
        }
    }
}
