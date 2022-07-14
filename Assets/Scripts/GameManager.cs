using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class GameManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public GameObject PlayerPrefab;
    public GameObject FieldControllerPrefab;

    public GameObject PreStartPanel;
    public GameObject EndPanel;

    public GameObject LeaveButton;

    public GameObject TimerText;

    private double _startTime;
    private double _lastTime;
    private bool _stopTime;

    private TextMeshProUGUI _preStartText;
    private Room _room;

    private void Start()
    {
        _startTime = PhotonNetwork.Time;
        _lastTime = PhotonNetwork.Time;
        _stopTime = true;

        _room = PhotonNetwork.CurrentRoom;

        PreStartPanel.SetActive(true);
        _preStartText = PreStartPanel.GetComponentInChildren<TextMeshProUGUI>();

        UpdatePreStartText();

        PreStartPanel.GetComponentInChildren<Button>().interactable = PhotonNetwork.IsMasterClient;

        PhotonPeer.RegisterType(typeof(Vector2Int), 0, SerializeVector2Int, DeserializeVector2Int);
    }
    private void Update()
    {
        if (!_stopTime && _lastTime + 1f < PhotonNetwork.Time)
        {
            TimerText.GetComponent<TextMeshProUGUI>().text = $"{(int)(PhotonNetwork.Time - _startTime)}";
            _lastTime = PhotonNetwork.Time;
        }
    }

    public void OnStartButton()
    {
        _stopTime = false;

        _room.IsOpen = false;

        var eventOptions = new RaiseEventOptions() { Receivers = ReceiverGroup.All };
        var sendOptions = new SendOptions() { Reliability = true };
        PhotonNetwork.RaiseEvent((byte)Event.Start, new System.Random().Next(int.MaxValue), eventOptions, sendOptions);
    }
    public void OnEndButton(bool isEnd)
    {
        var eventOptions = new RaiseEventOptions() { Receivers = ReceiverGroup.All };
        var sendOptions = new SendOptions() { Reliability = true };
        PhotonNetwork.RaiseEvent((byte)Event.End, isEnd, eventOptions, sendOptions);
    }

    public void OnLeaveButton()
    {
        Leave();
    }

    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case (byte)Event.Start:
                PreStartPanel.SetActive(false);

                UnityEngine.Random.InitState((int)photonEvent.CustomData);

                Instantiate(FieldControllerPrefab);

                var position = new Vector3(new System.Random().Next(0, Constants.FieldWidth),
                                           new System.Random().Next(0, Constants.FieldHeight));
                PhotonNetwork.Instantiate(PlayerPrefab.name, position, Quaternion.identity);
                break;
            case (byte)Event.Boom:
                EndGame($"{(string)photonEvent.CustomData} BOOMED");
                break;
            case (byte)Event.Win:
                EndGame("You WON!");
                break;
            case (byte)Event.End:
                if ((bool)photonEvent.CustomData)
                {
                    Leave();
                }
                else
                { 
                    PhotonNetwork.LoadLevel("Game");
                }
                break;
        }    
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(0);
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePreStartText();
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePreStartText();
    }

    public static object DeserializeVector2Int(byte[] data) =>
        new Vector2Int(BitConverter.ToInt32(data, 0), BitConverter.ToInt32(data, 4));
    public static byte[] SerializeVector2Int(object obj) =>
        BitConverter.GetBytes(((Vector2Int)obj).x).Concat(BitConverter.GetBytes(((Vector2Int)obj).y)).ToArray();

    private void Leave()
    {
        PhotonNetwork.LeaveRoom();
    }

    private void UpdatePreStartText()
    {
        _preStartText.text = $"   Players: {_room.PlayerCount}/{_room.MaxPlayers}";
        _room.Players
            .OrderBy(pair => pair.Key)
            .ToList()
            .ForEach(pair => _preStartText.text += $"\n{pair.Value.ActorNumber + ")",-4}{pair.Value.NickName,-25}");
    }

    private void EndGame(string EndText)
    {
        _stopTime = true;
        EndPanel.SetActive(true);

        EndPanel.GetComponentsInChildren<TextMeshProUGUI>()
            .First(TMP => new Regex(".*EndGame.*").IsMatch(TMP.gameObject.name)).text = EndText;
        EndPanel.GetComponentsInChildren<TextMeshProUGUI>()
            .First(TMP => new Regex(".*Waiting.*").IsMatch(TMP.gameObject.name)).gameObject.SetActive(!PhotonNetwork.IsMasterClient);

        EndPanel.GetComponentsInChildren<Button>()
            .ToList()
            .ForEach(button => button.gameObject.SetActive(PhotonNetwork.IsMasterClient));
    }
}
