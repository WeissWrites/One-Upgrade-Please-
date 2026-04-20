using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MysteryBoxManager : MonoBehaviour
{
    public static MysteryBoxManager Instance { get; private set; }

    [Header("Box Stations")]
    public List<GameObject> boxStations;

    [Header("Timings")]
    public float waitBeforeRelocate = 30f;

    private GameObject currentStation;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        foreach (GameObject station in boxStations)
            station.SetActive(false);

        if (boxStations.Count > 0)
        {
            currentStation = boxStations[Random.Range(0, boxStations.Count)];
            currentStation.SetActive(true);
        }
    }

    public void OnBoxClosed()
    {
        if (currentStation != null)
        {
            currentStation.SetActive(false);
        }
        StartCoroutine(RelocateRoutine());
    }

    private IEnumerator RelocateRoutine()
    {
        GameObject lastStation = currentStation;
        if (currentStation != null) currentStation.SetActive(false);

        yield return new WaitForSeconds(waitBeforeRelocate);

        int newIndex;
        do { newIndex = Random.Range(0, boxStations.Count); }
        while (boxStations[newIndex] == lastStation && boxStations.Count > 1);

        currentStation = boxStations[newIndex];
        currentStation.SetActive(true);
    }
}
