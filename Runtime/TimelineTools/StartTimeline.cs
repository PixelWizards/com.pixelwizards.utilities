using UnityEngine;
using UnityEngine.Playables;

public class StartTimeline : MonoBehaviour
{
    // Start is called before the first frame update
    public PlayableDirector masterTimeline;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            masterTimeline.Play();
            Debug.Log("Starting master timeline");
        }
    }
}
