using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class TimelineControls : MonoBehaviour
{
    public GameObject panel;
    public PlayableDirector masterTimeline;

    public GameObject playButton;
    public GameObject pauseButton;

    public TMPro.TextMeshProUGUI timeLabel;

    public void HandleInput(Button button)
    {
        var activeClick = button.name;
        Debug.Log("Clicked button: " + activeClick);

        switch( activeClick)
        {
            case "bPlay":
                {
                    masterTimeline.Play();
                    playButton.SetActive(false);
                    pauseButton.SetActive(true);
                    break;
                }
            case "pPause":
                {
                    masterTimeline.Pause();
                    playButton.SetActive(true);
                    pauseButton.SetActive(false);
                    break;
                }
            case "bPrev":
                {
                    masterTimeline.time = 0f;
                    break;
                }
            case "bNext":
                {
                    var duration = masterTimeline.duration;
                    masterTimeline.time = duration;
                    break;
                }
            case "bBackward":
                {
                    var currentTime = masterTimeline.time - 10f;
                    if (currentTime < 0)
                        currentTime = 0f;
                    masterTimeline.time = currentTime;
                    break;
                }
            case "bForward":
                {
                    var currentTime = masterTimeline.time + 10f;
                    if (currentTime > masterTimeline.duration)
                        currentTime = masterTimeline.duration;
                    masterTimeline.time = currentTime;
                    break;
                }
        }
    }

    public void Update()
    {
        timeLabel.text = "Time: " + masterTimeline.time;
    }
}
