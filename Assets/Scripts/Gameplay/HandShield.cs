using UnityEngine;
using System;

namespace Amapolas.Gameplay
{
    public class HandShield : MonoBehaviour
    {
        public event Action OnKnifeBlocked;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Knife"))
            {
                // We let the Knife handle its own deflection logic based on the tag
                // but we signal the blocking manager for feedback
                OnKnifeBlocked?.Invoke();
            }
        }
    }
}
