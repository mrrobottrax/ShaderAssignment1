using System.Collections.Generic;
using UnityEngine;

public class ActorManager : MonoBehaviour
{
    public static ActorManager Instance;

    [Header("Tick speed")]
    [SerializeField] private float _tps;

    [Header("System")]
    private HashSet<ActorBase> actors;
    private float m_nextTickTime = Time.time;


    private void Awake()
    {
        Instance = this;

        m_nextTickTime += (1/_tps);
    }

    public void AddActor(ActorBase baseActor)
    {
        actors.Add(baseActor);
    }

    private void Update()
    {
        if (Time.time > m_nextTickTime)
        {
            m_nextTickTime += (1/_tps);

            foreach (var i in actors)
            {
                i.TickUpdate();
            }
        }
    }
}
