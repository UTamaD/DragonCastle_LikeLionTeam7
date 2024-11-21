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


    private int _playerTemplateNum = 0;
    
    void Start()
    {
        CharSelectButtons[0].onClick.AddListener(() => SelectPlayerTemplate(0));
        CharSelectButtons[1].onClick.AddListener(() => SelectPlayerTemplate(1));
        CharSelectButtons[2].onClick.AddListener(() => SelectPlayerTemplate(2));
        
        StartButton.onClick.AddListener(() =>
        {
            if (IdField.text == null)
                return;
            SuperManager.Instance.playerId = IdField.text;
            TcpProtobufClient.Instance.SendLoginMessage(IdField.text, _playerTemplateNum);
            UICanvas.SetActive(false);
        });
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
