using System;
using UnityEngine;
using Navigation;
using System.Collections.Generic;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private GameObject TargetCursorPrefab = null;
    private GameObject TargetCursor = null;
    [SerializeField]
    private GameObject UnitPrefab = null;
    [SerializeField]
    private Transform PlayerStart = null;
    private Unit CurrentLeader;
    private List<Unit> followers = new List<Unit>();

    private Action OnMouseClicked;

    private GameObject GetTargetCursor()
    {
        if (TargetCursor == null)
            TargetCursor = Instantiate(TargetCursorPrefab);
        return TargetCursor;
    }

    Unit SpawnUnit()
    {
        GameObject unitInst = Instantiate(UnitPrefab, PlayerStart, false);
        unitInst.transform.parent = null;
        Unit currentUnit = unitInst.GetComponent<Unit>();
        if (currentUnit == null)
        {
            Debug.LogError("Could not find component Unity in unit instance");
        }

        RaycastHit raycastInfo;
        Ray ray = new Ray(unitInst.transform.position, Vector3.down);
        if (Physics.Raycast(ray, out raycastInfo, 10f, 1 << LayerMask.NameToLayer("Floor")))
        {
            unitInst.transform.position = raycastInfo.point;
        }
        return currentUnit;
    }

    Separation separationSteering;

    private void Start ()
    {
        if (UnitPrefab)
        {
            CurrentLeader = SpawnUnit();
            CurrentLeader.SetSelected(true);
            Group group = new Group();
            group.boids.Add(CurrentLeader.transform);

            for (int i = 0; i < 3; i++)
            {
                Unit follower = SpawnUnit();
                followers.Add(follower);
                group.boids.Add(follower.transform);
            }

            separationSteering = new Separation(group);
        }

        OnMouseClicked += () =>
        {
            int floorLayer = 1 << LayerMask.NameToLayer("Floor");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit raycastInfo;
            // unit move target
            if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, floorLayer))
            {
                Vector3 newPos = raycastInfo.point;
                Vector3 targetPos = newPos;
                targetPos.y += 0.1f;
                GetTargetCursor().transform.position = targetPos;

                if (TileNavGraph.Instance.IsPosValid(newPos))
                {
                    CurrentLeader.SetTargetPos(newPos);
                }
            }
        };

        StartCoroutine(UpdateLineFormation());
    }

    IEnumerator UpdateLineFormation()
    {
        while (true)
        {
            for (int i = 0; i < followers.Count; i++)
            {
                Unit follower = followers[i];
                float dist = 0.2f;
                follower.SetTargetPos(separationSteering.ApplySteering(follower.transform) + CurrentLeader.transform.position + CurrentLeader.transform.right * (i - followers.Count / 2) * dist);
            }

            yield return new WaitForSeconds(1f);
        }
    }

    int j = 0;
    private void Update ()
    {
        if (Input.GetMouseButtonDown(0))
            OnMouseClicked();


        //j++;

        //if (j % 30 * 5 != 0)
        //    return;

        //for (int i = 0; i < followers.Count; i++) 
        //{
        //    Unit follower = followers[i];
        //    float dist = 0.2f;
        //    follower.SetTargetPos(CurrentLeader.transform.position + CurrentLeader.transform.right * (i - followers.Count/2) * dist);
        //    //follower.SetTargetPos(new Vector3(i * 10, 0, 0));
        //}
    }
}
