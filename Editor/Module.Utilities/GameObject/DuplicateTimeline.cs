using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PixelWizards.Utilities
{
    public class DuplicateTimeline
    {
        [MenuItem("GameObject/Duplicate Selected Timeline", false, 0)]
        public static void DuplicateSelectedTimeline()
        {
            // grab the director from the active selection
            var director = Selection.activeGameObject.GetComponent<PlayableDirector>();
            if (director == null)
                return;

            // figure out what the timeline asset for the director is
            var sourceTimelineAsset = director.playableAsset as TimelineAsset;
            if (sourceTimelineAsset == null)
                return;

            // duplicate timeline asset
            var sourceAssetPath = AssetDatabase.GetAssetPath(sourceTimelineAsset);
            var destinationAssetPath = AssetDatabase.GenerateUniqueAssetPath(sourceAssetPath);
            AssetDatabase.CopyAsset(sourceAssetPath, destinationAssetPath);

            var newTimelineAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(destinationAssetPath);

            // duplicate the scene playable
            var newTimelineGO = Object.Instantiate(director.gameObject);
            var newDirector = newTimelineGO.GetComponent<PlayableDirector>();
            newDirector.playableAsset = newTimelineAsset;

            // troll through the new timeline and re-hook up all its track bindings
            var originalTracks = new List<TrackAsset>(sourceTimelineAsset.GetRootTracks());
            var newTracks = new List<TrackAsset>(newTimelineAsset.GetRootTracks());
            foreach (var track in sourceTimelineAsset.GetRootTracks())
            {
                originalTracks.AddRange(GetChildTracksRecursive(track));
            }
            foreach (var track in newTimelineAsset.GetRootTracks())
            {
                newTracks.AddRange(GetChildTracksRecursive(track));
            }

            foreach (var track in newTracks)
            {
                var sourceTrack = FindTrack(originalTracks, track.name);
                if (sourceTrack != null)
                {
                    newDirector.SetGenericBinding(track, director.GetGenericBinding(sourceTrack));
                }
            }
        }

        [MenuItem("GameObject/Duplicate Selected Timeline", true, 0)]
        public static bool ValidateDuplicateSelectedTimeline()
        {
            if (Selection.activeGameObject == null) return false;
            var director = Selection.activeGameObject.GetComponent<PlayableDirector>();
            if (director == null) return false;

            return true;
        }

        /// <summary>
        /// finds all of the child tracks recursively (group tracks etc are considered parent tracks)
        /// </summary>
        /// <param name="track">parent track</param>
        /// <returns>list of child tracks (if any exist)</returns>
        private static IEnumerable<TrackAsset> GetChildTracksRecursive(TrackAsset track)
        {
            var tracks = new List<TrackAsset>();
            foreach (var childTrack in track.GetChildTracks())
            {
                if (childTrack.GetChildTracks() != null)
                {
                    tracks.AddRange(GetChildTracksRecursive(childTrack));
                }
                tracks.Add(childTrack);
            }

            return tracks;
        }

        /// <summary>
        /// Finds a given track by name
        /// </summary>
        /// <param name="tracks">list of tracks to search</param>
        /// <param name="name">name of the track we're looking for</param>
        /// <returns></returns>
        private static TrackAsset FindTrack(List<TrackAsset> tracks, string name)
        {
            foreach (var track in tracks)
            {
                if (track.name == name) return track;
            }

            Debug.LogWarning($"Could not find track in original Timeline with name: {name}");
            return null;
        }
    }
}