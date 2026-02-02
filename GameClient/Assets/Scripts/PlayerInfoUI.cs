using TMPro;
using UnityEngine;

public class PlayerInfoUI : MonoBehaviour
{
    public TMP_Text UserName;

    public void SetName(string name)
    {
        UserName.text = name;
    }
}
