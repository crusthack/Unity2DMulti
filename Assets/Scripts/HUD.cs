using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public TMP_Text ScoreText;
    public Slider PassiveSkill;
    public TMP_Text PassiveName;
    public Slider PassiveDuration;
    public Slider ActiveSkill;
    public TMP_Text ActiveName;
    public Slider ActiveDuration;

    Player GamePlayer;
    SkillSystem PlayerSkills;

    public void BindPlayer(GameObject playerObj)
    {
        GamePlayer = playerObj.GetComponent<Player>();
        PlayerSkills = playerObj.GetComponent<SkillSystem>();
        PassiveName.text = PlayerSkills.PassiveSkill.SkillName;
        ActiveName.text = PlayerSkills.ActiveSkill.SkillName;
    }

    void Start()
    {
        PassiveDuration.gameObject.SetActive(false);
        ActiveDuration.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (GamePlayer == null)
        {
            return;
        }

        ScoreText.text = "Score: " + GamePlayer.Score.ToString("F0");
        PassiveSkill.value = GetSkillCooldownProgress(PlayerSkills.PassiveSkillInstance);
        ActiveSkill.value = GetSkillCooldownProgress(PlayerSkills.ActiveSkillInstance);

        if (PlayerSkills.PassiveSkillInstance.IsEnabled)
        {
            PassiveDuration.gameObject.SetActive(true);
            float elapsed = PlayerSkills.PassiveSkillInstance.ActiveDuration - (Time.time - PlayerSkills.PassiveSkillInstance.LastActiveTime);
            PassiveDuration.value = elapsed / PlayerSkills.PassiveSkillInstance.ActiveDuration;
        }
        else
        {
            PassiveDuration.gameObject.SetActive(false);
        }

        if (PlayerSkills.ActiveSkillInstance.IsEnabled)
        {
            ActiveDuration.gameObject.SetActive(true);
            float elapsed = PlayerSkills.ActiveSkillInstance.ActiveDuration - (Time.time - PlayerSkills.ActiveSkillInstance.LastActiveTime);
            ActiveDuration.value = elapsed / PlayerSkills.ActiveSkillInstance.ActiveDuration;
        }
        else
        {
            ActiveDuration.gameObject.SetActive(false);
        }
    }

    float GetSkillCooldownProgress(Skill skill)
    {
        if(skill == null || skill.Cooldown < 0)
        {
            return 1f;
        }

        return (skill.Cooldown - (Time.time - skill.LastActiveTime)) / skill.Cooldown;
    }
}
