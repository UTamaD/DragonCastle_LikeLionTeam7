using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TMPro.TMP_InputField IdField;
    
    void Start()
    {
        IdField.onSubmit.AddListener((string id) =>
        {
            SuperManager.Instance.playerId = id;
            TcpProtobufClient.Instance.SendLoginMessage(id);
            IdField.gameObject.SetActive(false);
        });
    }
}
