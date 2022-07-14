using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public GameObject ConnectPanel;

    public GameObject ErrorPanel;
    public GameObject ErrorText;

    public GameObject OptionPanel;

    public GameObject ParametersPanel;
    public GameObject AlternativeRotationToggle;

    public GameObject NameText;
    public GameObject RoomText;

    private string _roomAction;

    private void Start()
    {
        LoadParameters();

        Screen.SetResolution(768, 480, FullScreenMode.Windowed);
        ConnectPanel.SetActive(true);

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "1";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        ConnectPanel.SetActive(false);
    }
    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("Game");
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        ErrorPanel.SetActive(true);
        ErrorText.GetComponent<TextMeshProUGUI>().text = message;
    }

    public void OnOptionButton() => OptionPanel.SetActive(true);

    public void OnRoomButton(string action)
    {
        ParametersPanel.SetActive(true);
        _roomAction = action;
    }

    public void OnOKButton()
    {
        var nameText = NameText.GetComponent<TextMeshProUGUI>().text;
        var name = nameText != "" && nameText[0] != Constants.InvisibleChar ? nameText : "Player";
        PhotonNetwork.NickName = $"{name}#{Random.Range(1, 1000)}";

        var roomText = RoomText.GetComponent<TextMeshProUGUI>().text;
        var room = roomText != "" && roomText[0] != Constants.InvisibleChar ? roomText : Random.Range(1, 1000).ToString();
        if (_roomAction == "Create")
        {
            PhotonNetwork.CreateRoom(room , new Photon.Realtime.RoomOptions { MaxPlayers = 4 });
        }
        else if (_roomAction == "Join")
        {
            PhotonNetwork.JoinRoom(room);
        }
    }

    public void OnSaveParametersButton()
    {
        SaveParameters();
    }

    public void OnParametersCancelButton()
    {
        ParametersPanel.SetActive(false);
        _roomAction = "";
    }
    public void OnOptionsCancelButton()
    {
        OptionPanel.SetActive(false);
    }
    public void OnErrorCancelButton()
    {
        ErrorPanel.SetActive(false);
        ErrorText.GetComponent<TextMeshProUGUI>().text = string.Empty;
    }

    public void LoadParameters()
    {
        var rotationOption = PlayerPrefs.GetString(Utilities.SaveTagToString(SaveTag.AlternativeRotation), false.ToString());
        AlternativeRotationToggle.GetComponent<Toggle>().isOn = rotationOption == true.ToString();
    }
    public void SaveParameters()
    {
        PlayerPrefs.SetString(Utilities.SaveTagToString(SaveTag.AlternativeRotation), AlternativeRotationToggle.GetComponent<Toggle>().isOn.ToString());
    }
}
