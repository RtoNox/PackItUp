using UnityEngine;

public interface IAbilityCooldown
{
    string AbilityName { get; }
    Sprite AbilityIcon { get; }
    float CooldownDuration { get; }
    KeyCode AssignedKey { get; }
    float GetRemainingCooldown();
}