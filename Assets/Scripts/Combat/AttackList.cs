using UnityEngine;

[System.Serializable]
public class AttackList
{
    // Array of attack groups
    [field: SerializeField] public AttackGroup[] AttackGroups { get; private set; }

    #region Helper Methods
    /// <summary>
    /// Retrieves an attack group by its title.
    /// </summary>
    /// <param name="groupTitle">The title of the attack group.</param>
    /// <returns>The attack group with the given title, or null if not found.</returns>
    public AttackGroup GetAttackGroup(string groupTitle)
    {
        foreach (AttackGroup group in AttackGroups)
        {
            if (group.GroupTitle == groupTitle)
                return group;
        }

        Debug.Log("NOTHING FOUND");
        return null; // Return null if no group with the given title is found
    }

    /// <summary>
    /// Retrieves an attack from a specific group by attack ID.
    /// </summary>
    /// <param name="attackGroup">The attack group.</param>
    /// <param name="attackID">The ID of the attack.</param>
    /// <returns>The attack with the given ID from the specified group, or null if not found.</returns>
    public Attack GetAttackFromGroup(AttackGroup attackGroup, int attackID)
    {
        return attackGroup?.GetAttack(attackID);
    }
    #endregion

    #region Nested Classes
    [System.Serializable]
    public class Attack
    {
        // The data associated with this attack
        [field: SerializeField] public AttackData AttackData { get; private set; }
    }

    [System.Serializable]
    public class AttackGroup
    {
        // The title of the attack group
        [field: SerializeField, Tooltip("The title of the group referenced by the attack animation")]
        public string GroupTitle { get; private set; }

        // Array of attacks in this group
        [field: SerializeField] public Attack[] Attacks { get; private set; }

        /// <summary>
        /// Retrieves an attack by its ID.
        /// </summary>
        /// <param name="attackID">The ID of the attack.</param>
        /// <returns>The attack with the given ID, or null if not found.</returns>
        public Attack GetAttack(int attackID)
        {
            return Attacks != null && attackID >= 0 && attackID < Attacks.Length ? Attacks[attackID] : null;
        }
    }
    #endregion
}