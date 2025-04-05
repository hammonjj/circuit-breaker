using System;
using Platformer.Gameplay;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// Represebts the current vital statistics of some game entity.
    /// </summary>
    public class Health : MonoBehaviour
    {
        public int maxHP = 1;
        public bool IsAlive => currentHP > 0;

        int currentHP;

        public void IncrementHealth()
        {
            currentHP = Mathf.Clamp(currentHP + 1, 0, maxHP);
        }

        public void DecrementHealth()
        {
            currentHP = Mathf.Clamp(currentHP - 1, 0, maxHP);
            if (currentHP == 0)
            {
                var ev = Schedule<HealthIsZero>();
                ev.health = this;
            }
        }

        /// <summary>
        /// Decrement the HP of the entitiy until HP reaches 0.
        /// </summary>
        public void Die()
        {
            while (currentHP > 0) DecrementHealth();
        }

        void Awake()
        {
            currentHP = maxHP;
        }
    }
}
