using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PixelWizards.MultiScene.Timeline
{
	public class MultiSceneSwapAsset : PlayableAsset, ITimelineClipAsset
	{
        [Header("Should we unload existing scenes?")]
        public bool unloadExisting;

		[Header("Should we use Async loading or not?")]
		public bool useAsyncLoading = false;


		[Header("Which scene configs do we want to load?")]
        public List<string> loadConfigs = new List<string>();

        [Header("Which scene configs do we want to unload?")]
        public List<string> unloadConfigs = new List<string>();
        

		public ClipCaps clipCaps
		{
			get { return ClipCaps.None; }
		}

		public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
		{
			var playable = ScriptPlayable<MultiSceneSwapBehaviour>.Create(graph);
			
			var sceneSwapBehaviour = playable.GetBehaviour();

			sceneSwapBehaviour.useAsyncLoading = useAsyncLoading;
            sceneSwapBehaviour.loadConfigs = loadConfigs;
            sceneSwapBehaviour.unloadConfigs = unloadConfigs;
            sceneSwapBehaviour.unloadExisting = unloadExisting;

			return playable;   
		}
	}
}