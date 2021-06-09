using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelWizards.Utilities
{
    public class SetTargetFramerate : MonoBehaviour
    {
        public int targetFramerate = 60;

        // Start is called before the first frame update
        private void Awake()
        {
            Application.targetFrameRate = targetFramerate;
        }
    }
}