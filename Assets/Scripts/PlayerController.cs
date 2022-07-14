using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class PlayerController : MonoBehaviour, IPunObservable
{
    private Vector2Int _gamePosition;

    private bool _isAlternativeRotation;

    public Vector2Int GamePosition
    {
        get => _gamePosition;
        private set 
        {
            if (value.x < 0) value.x = 0;
            if (value.x >= Constants.FieldWidth) value.x = Constants.FieldWidth - 1;

            if (value.y < 0) value.y = 0;
            if (value.y >= Constants.FieldHeight) value.y = Constants.FieldHeight - 1;

            _gamePosition = value;
            transform.localPosition = ConvertVector2IntTo3(value);
        }
    }
    public Vector2Int Target => GamePosition + ConvertVector3To2Int(ConvertRotationToMove(transform.rotation.eulerAngles));

    public PhotonView PhotonView { get; private set; }
    public int Score;

    private void Start()
    {
        PhotonView = GetComponent<PhotonView>();
        GetComponentInChildren<SpriteRenderer>().color = PhotonView.IsMine ? Color.blue : Color.red;

        GamePosition = ConvertVector3To2Int(transform.localPosition);

        FindObjectOfType<FieldController>().AddPlayer(this);

        if (!PhotonView.IsMine) return;

        _isAlternativeRotation = PlayerPrefs.GetString(Utilities.SaveTagToString(SaveTag.AlternativeRotation), false.ToString()) == true.ToString();
    }
    private void Update()
    {
        UserInput();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(GamePosition);
            stream.SendNext(transform.rotation);
        }
        else
        {
            GamePosition = (Vector2Int)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();
        }
    }

    private void UserInput()
    {
        if (!PhotonView.IsMine) return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) UserMove(Vector3.left);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) UserMove(Vector3.right);
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) UserMove(Vector3.up);
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) UserMove(Vector3.down);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            GamePosition += ConvertVector3To2Int(2 * ConvertRotationToMove(transform.rotation.eulerAngles));
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            OnSwitchFlag(Target.x, Target.y);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            OnAoePress(GamePosition.x, GamePosition.y);
        }
    }
    private void UserMove(Vector3 direction)
    {
        if (_isAlternativeRotation)
        {
            if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                GamePosition += ConvertVector3To2Int(direction);
            }
        }
        else
        {
            if (transform.rotation == Quaternion.Euler(ConvertMoveToRotation(direction)))
            {
                GamePosition += ConvertVector3To2Int(direction);
            }
        }
        transform.rotation = Quaternion.Euler(ConvertMoveToRotation(direction));
    }

    private Vector3 ConvertMoveToRotation(Vector3 move) =>
        new Vector3(0,0, 180 - (Math.Abs(move.y) + move.y - move.x) * 90);
    private Vector3 ConvertRotationToMove(Vector3 rotation) =>
        new Vector3(((int)rotation.z - 180) % 180 / 90, Math.Abs((int)rotation.z - 180) / 90 - 1);

    private Vector2Int ConvertVector3To2Int(Vector3 vector) =>
        new Vector2Int((int)vector.x, (int)vector.y);
    private Vector3 ConvertVector2IntTo3(Vector2Int vector) =>
        new Vector3(vector.x, vector.y);

    public event Action<int, int> OnSwitchFlag;
    public event Action<int, int> OnAoePress;
}

