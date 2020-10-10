using System.Text;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

[ExecuteAlways]
public class HUD : MonoBehaviour
{
#if UNITY_EDITOR
    private PlayableDirector[] timelinesInScene;
    public Text hudText;
    public StringBuilder output;

    // Start is called before the first frame update
    private void OnEnable()
    {
        // cache the hud element, grab from this object if it's null
        if( hudText == null)
        {
            hudText = GetComponent<Text>();
        }
        
        // use stringbuilder to write the output
        output = new StringBuilder();
    }

    /// <summary>
    /// run our loop and update the hud
    /// </summary>
    private void Update()
    { 
        // really shouldn't do this every frame, but we have new timelines potentially 
        // being activated at any given point (with control tracks / activation clips etc), 
        // so we kind of need to check all of the time
        timelinesInScene = FindObjectsOfType<PlayableDirector>();
       // Debug.Log("Found : " + timelinesInScene.Length + " PlayableDirectors");

        // clear the output for the new frame
        output.Clear();

        foreach (var director in timelinesInScene)
        {
            output.AppendLine("Timeline: " + director.name);
            var currentTime = director.time;
            var ta = (TimelineAsset)director.playableAsset;

            var tracks = ta.GetOutputTracks();
            foreach( var track in tracks)
            {
                if (track.muted)
                    continue;

                var clips = track.GetClips();
                foreach( var clip in clips)
                {
                    var startTime = clip.start;
                    var endTime = clip.end;

                    if( startTime <= currentTime && currentTime <= endTime)
                    {
                        output.AppendLine("    " + clip.displayName);
                    }
                }
            }
            
        }

        hudText.text = output.ToString();
    }
#endif
}
