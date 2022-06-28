using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    [Header("Active-Vars")]
    public List<Unit> ActiveUnits = new List<Unit>(); // Exclusive to movable units, not buildings or resources, used primary for formations and actions
    public List<Unit> Units = new List<Unit>();

    // Private Vars

    private Camera c;

    RaycastHit hit;
    Ray ray;

    float dt = 0.25f; // dt & tdc are used for double clicking
    double tdc = 0f;

    void Start()
    {
        c = FindObjectOfType<Camera>();
    }

    // Delta Calc.

    Vector2 mouseDelta; // Used to fix a bug where SelectSingluarUnit function is called whilst box selection is active, causing issues

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
        }
        if (Input.GetMouseButton(0))
        {
            UpdateSelectionBox(Input.mousePosition);
            mouseDelta = new Vector2(Input.mousePosition.x - startPos.x, Input.mousePosition.y - startPos.y);
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (singleSelectedUnit != null) singleSelectedUnit = null;

            ReleaseSelectionBox(); if (Mathf.Abs(mouseDelta.x + mouseDelta.y) < 4) SelectSingluarUnit();

            // DOUBLE CLICK

            if ((Time.time - tdc) > dt) { } else { SelectAllType(); }

            tdc = Time.time;
        }
        if (Input.GetKeyUp(KeyCode.Mouse1) && ActiveUnits.Count > 0) // Action
        {
            ActionManager();
        }
    }

    // SELECTION BOX

    [Header("Selection-Box")]

    public RectTransform selectionBox;
    private Vector2 startPos;

    void UpdateSelectionBox(Vector2 curMousePos)
    {
        if (!selectionBox.gameObject.activeInHierarchy)
        { selectionBox.gameObject.SetActive(true); }
        float width = curMousePos.x - startPos.x;
        float height = curMousePos.y - startPos.y;
        selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        selectionBox.anchoredPosition = startPos + new Vector2(width / 2, height / 2);
    }

    void ReleaseSelectionBox()
    {
        selectionBox.gameObject.SetActive(false);

        Vector2 min = selectionBox.anchoredPosition - (selectionBox.sizeDelta / 2);
        Vector2 max = selectionBox.anchoredPosition + (selectionBox.sizeDelta / 2);

        bool PrioritizedUnitsExist = false; // Do prioritized units exist in within the selection box?

        for (int i = 0; i < Units.Count; i++)
        {
            Vector3 screenPos = FindObjectOfType<Camera>().WorldToScreenPoint(Units[i].transform.position);

            if (screenPos.x > min.x && screenPos.x < max.x && screenPos.y > min.y && screenPos.y < max.y)
            {
                if (Units[i].UnitType != UnitType.Building && Units[i].UnitType != UnitType.Resource)
                {
                    if (!Units[i].Selected) Units[i].Select();
                    if (Units[i].UnitType == UnitType.Unit) PrioritizedUnitsExist = true;
                }
            }
            else
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                    if (Units[i].Selected) Units[i].UnSelect();           
            }

            if (Units[i].UnitType == UnitType.Resource) Units[i].UnSelect(); // Resources are standalones whenever selected
        }

        if (PrioritizedUnitsExist && !Input.GetKey(KeyCode.LeftShift)) foreach (Unit Unit in Units) { if (Unit.UnitType == UnitType.Worker) Unit.UnSelect(); }
    }

    // SELECT FUNCTIONS

    // Select Singular Vars

    private Unit singleSelectedUnit;

    void SelectSingluarUnit()
    {
        ray = c.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit))
        {
            Transform t = hit.transform;
            if (t.GetComponent<Unit>())
            {
                Unit u = t.GetComponent<Unit>(); singleSelectedUnit = u;

                if (!Input.GetKey(KeyCode.LeftShift) || u.UnitType == UnitType.Resource)
                {
                    for (int i = 0; i < Units.Count; i++)
                    {
                        if (Units[i].Selected) Units[i].UnSelect();
                    }
                }

                if (!u.Selected) u.Select();
                else u.UnSelect();
            }
        }
    }

    void SelectAllType() //Select all of one type
    {
        if (singleSelectedUnit != null && singleSelectedUnit.UnitType != UnitType.Resource)
        {
            string un = singleSelectedUnit.UnitName;

            for (int i = 0; i < Units.Count; i++)
            {
                if (Units[i].UnitName == un)
                    Units[i].Select();
                else if (!Input.GetKey(KeyCode.LeftShift))
                    Units[i].UnSelect();
            }
        }
    }

    // ACTION FUNCTIONS

    void ActionManager()
    {
        ray = c.ScreenPointToRay(Input.mousePosition);
        Vector3 CentralPosition = Vector3.zero; // =null

        if (Physics.Raycast(ray, out hit))
        {
            CentralPosition = hit.point;
        }

        if (CentralPosition != Vector3.zero)
        {
            if (ActiveUnits.Count > 1)
            {
                int count = ActiveUnits.Count;
                //List<Vector3> DeterminedDestinations = null;

                for (int c = 0; c < ActiveUnits.Count; c++)
                {
                    Unit Unit = ActiveUnits[c];

                    Vector3 StartingPosition = new Vector3(0, 0, CentralPosition.z - (c / 2) * 2);
                    Vector3 DeterminedDestination = new Vector3(CentralPosition.x + c * 2, Unit.transform.position.y, StartingPosition.z + c * 2);
                    //DeterminedDestinations.Add(DeterminedDestination);

                    Unit.Agent.SetDestination(DeterminedDestination);
                    //ActiveUnits[c].GetComponent<UnityEngine.AI.NavMeshAgent>().SetDestination(DestinationPosition);
                }
            }
            else
            {
                ActiveUnits[0].GetComponent<UnityEngine.AI.NavMeshAgent>().SetDestination(CentralPosition);
            }
        }
    }
}
