using NetworkController.Message;
using Protos;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Chatting : MonoBehaviour
{
    public TMP_InputField InputField;
    public GameObject ChatProps;
    public Transform Content;
    public Scrollbar ChatScroll;
    public ScrollRect Rect;

    void Awake()
    {
        InputField.onSelect.AddListener(OnChatFocused);
        InputField.onEndEdit.AddListener(OnChatCommited);

        GameManager.Instance.NetworkManager.OnMessageRecv += HandleMessage;
    }

    private void OnDestroy()
    {
        InputField.onSelect.RemoveListener(OnChatFocused);
        InputField.onEndEdit.RemoveListener(OnChatCommited);

        GameManager.Instance.NetworkManager.OnMessageRecv -= HandleMessage;
    }

    public void OnChatFocused(string text)
    {
        GameManager.Instance.GamePlayer.GetComponent<Player>().DisableInput();
        Debug.Log("Focused");
    }

    public void OnChatCommited(string text)
    {
        bool enter =
            Keyboard.current != null &&
            Keyboard.current.enterKey.wasPressedThisFrame;

        GameManager.Instance.GamePlayer
            .GetComponent<Player>()
            .EnableInput();
            Debug.Log("Commited");

        if (enter)
        {
            GameManager.Instance.NetworkManager.SendChat(InputField.text);
            InputField.text = "";
        }
    }

    public void HandleMessage(ProtobufMessage message)
    {
        if((ProtobufMessage.OpCode)message.Header.OpCode == ProtobufMessage.OpCode.Chatting)
        {
            var msg = message.Payload as ChattingMessage;
            var payload = msg.Username + ": " + msg.Message;
            Debug.Log(payload);
            var chat = Instantiate(ChatProps, Content);
            chat.GetComponent<TMP_Text>().text = payload;
            ChatScroll.value = 0;
            Debug.Log(ChatScroll.value);

            Canvas.ForceUpdateCanvases();
            Rect.verticalNormalizedPosition = 0f;

        }
    }
}
