using UnityEngine;

public class Potal : MonoBehaviour
{
    public string TargetMap;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GameManager.Instance.TempScore = collision.GetComponent<Player>().Score;
            GameManager.Instance.CurrentMap = TargetMap;
            GameManager.Instance.GamePlayer.GetComponent<Player>().CurrentMap = TargetMap;
            UnityEngine.SceneManagement.SceneManager.LoadScene(TargetMap);
        }
    }
}
