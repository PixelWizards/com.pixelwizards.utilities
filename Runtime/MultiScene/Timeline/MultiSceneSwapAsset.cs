using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PixelWizards.MultiScene.Timeline
{
	public class MultiSceneSwapAsset : PlayableAsset, ITimelineClipAsset
	{
        public List<string> loadConfigs = new List<string>();
        public List<string> unloadConfigs = new List<string>();
        public bool unloadExisting;

		public ClipCaps clipCaps
		{
			get { return ClipCaps.None; }
		}

		public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
		{
			var playable = ScriptPlayable<MultiSceneSwapBehaviour>.Create(graph);
			
			var sceneSwapBehaviour = playable.GetBehaviour();
            
            sceneSwapBehaviour.loadConfigs = loadConfigs;
            sceneSwapBehaviour.unloadConfigs = unloadConfigs;
            sceneSwapBehaviour.unloadExisting = unloadExisting;

			return playable;   
		}
	}
}