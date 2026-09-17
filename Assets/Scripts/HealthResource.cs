using UnityEngine;

public class HealthResource : MonoBehaviour
{
    [Header("Recurso de salud")]
    public float healAmount = 30f;

    public bool available = true;

    public void Use(UtilityAgent agent)
    {
        if (!available || agent == null)
            return;

        agent.health += healAmount;
        agent.health = Mathf.Clamp(agent.health, 0f, agent.maxHealth);

        available = false;

        Debug.Log("HealthResource utilizado. Salud actual: " + agent.health);
    }
}