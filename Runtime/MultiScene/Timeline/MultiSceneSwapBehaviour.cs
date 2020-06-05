using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PixelWizards.MultiScene.Timeline
{
	public class MultiSceneSwapBehaviour : PlayableBehaviour
	{
        public bool unloadExisting;

        public bool useAsyncLoading = false;

        public List<string> loadConfigs = new List<string>();

        public List<string> unloadConfigs = new List<string>();

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
		{
			var sceneSwap = playerData as MultiSceneSwapHelper;

			if (sceneSwap != null)
			{
                foreach( var config in unloadConfigs)
                {
                    sceneSwap.UnloadConfig(config);
                }

                foreach (var config in loadConfigs)
                {
                    sceneSwap.LoadConfig(config, unloadExisting, useAsyncLoading);
                }

            }
        }
	}
}