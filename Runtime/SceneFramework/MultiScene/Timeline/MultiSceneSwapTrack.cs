using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PixelWizards.MultiScene.Timeline
{
	[TrackClipType(typeof(MultiSceneSwapAsset))]
	[TrackBindingType(typeof(MultiSceneSwapHelper))]
	public class MultiSceneSwapTrack : TrackAsset {}
}