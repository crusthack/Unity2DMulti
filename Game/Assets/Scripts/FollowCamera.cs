using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    void LateUpdate()
    {
        if (GameManager.Instance.GamePlayer != null)
        {
            var playerPos = GameManager.Instance.GamePlayer.transform.position;
            transform.position = new Vector3(playerPos.x, playerPos.y, transform.position.z);
        }
    }
}
