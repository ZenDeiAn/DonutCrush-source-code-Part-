using System.Collections.Generic;
using Airpass.DesignPattern;
using UnityEngine;
using UnityEngine.Playables;

public class CinemachineManager : SingletonUnity<CinemachineManager>
{
    [SerializeField] private List<PlayableDirector> playableDirectors;

    public void PlayTimeLine(int index)
    {
        if (playableDirectors.Count > index)
        {
            if (playableDirectors[index] != null)
            {
                playableDirectors[index].Play();
            }
        }
    }

    public void StopAllTimeLine()
    {
        foreach (var timeline in playableDirectors)
        {
            if (timeline != null)
            {
                timeline.Stop();
            }
        }
    }
}
