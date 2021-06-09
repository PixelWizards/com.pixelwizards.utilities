using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelWizards.Utilities
{
    /// <summary>
    /// Allows you to define the loading priority for async loading
    /// </summary>
    public class SetLoadingPriority : MonoBehaviour
    {
        [Header("Allows you to define the loading priority for async loading")]
        public ThreadPriority loadingPriority = ThreadPriority.Low;

        private void Awake()
        {
            Application.backgroundLoadingPriority = loadingPriority;
        }
    }
}