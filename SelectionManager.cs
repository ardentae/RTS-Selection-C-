using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    [Header("Debuging-Tools")]
    public Transform Destination; // Transform, as states, a transform that can be used to be customized through code (DON'T REMOVE)
    public GameObject FOV;

    [Header("Active-Vars")]
    public List<Unit> activeUnits = new List<Unit>(); // Exclusive to movable units, not buildings or resources, used primary for formations and actions
    public List<Unit> Units = new List<Unit>();

    // Private Vars

    private Camera c;

    RaycastHit hit;
    Ray ray;

    float dt = 0.25f; // dt & tdc are used for double clicking
    double tdc = 0f;

    void Start()
    {
        c = GetComponent<Camera>();
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
        if (Input.GetKeyUp(KeyCode.Mouse1) && activeUnits.Count > 0) // Action
        {
            ActionManager();
        }

        // DEBUG (CAN REMOVE)

        if (Input.GetKeyUp(KeyCode.P)) // Disable / Enable FOW
        {
            Projector p = FindObjectOfType<Projector>();

            if (!p.enabled) { p.enabled = true; } else { p.enabled = false; }
        }
        if (Input.GetKeyUp(KeyCode.L)) // Lighten map (unexplored.)
        {
            GameObject FOVobj = Instantiate(FOV, Vector3.zero, Quaternion.identity);
            FOVobj.transform.localScale = new Vector3(1000, 1000, 1000); Destroy(FOVobj, 0.1f);
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

        for (int i = 0; i < Units.Count; i++)
        {
            Vector3 screenPos = GetComponent<Camera>().WorldToScreenPoint(Units[i].transform.position);

            if (screenPos.x > min.x && screenPos.x < max.x && screenPos.y > min.y && screenPos.y < max.y)
            {
                if (Units[i].unitType != unitType.Building && Units[i].unitType != unitType.Resource)
                    if (!Units[i].selected) Units[i].Select();
            }
            else
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                    if (Units[i].selected) Units[i].UnSelect();
            }

            if (Units[i].unitType == unitType.Resource) Units[i].UnSelect(); // Resources are standalones whenever selected
        }
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

                if (!Input.GetKey(KeyCode.LeftShift) || u.unitType == unitType.Resource)
                {
                    for (int i = 0; i < Units.Count; i++)
                    {
                        if (Units[i].selected) Units[i].UnSelect();
                    }
                }

                if (!u.selected) u.Select();
                else u.UnSelect();
            }
        }
    }

    void SelectAllType() //Select all of one type
    {
        if (singleSelectedUnit != null && singleSelectedUnit.unitType != unitType.Resource)
        {
            string un = singleSelectedUnit.unitName;

            for (int i = 0; i < Units.Count; i++)
            {
                if (Units[i].unitName == un)
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
        Vector3 centralPosition = Vector3.zero; // =null

        if (Physics.Raycast(ray, out hit))
        {
            centralPosition = hit.point;
        }

        if (centralPosition != Vector3.zero)
        {
            // Resetting values

            int xpos, ypos; xpos = 1; ypos = 1;

            for (int c = 0; c < activeUnits.Count; c++) // Determine how many points to create
            {
                Transform point = Instantiate(Destination, centralPosition, Quaternion.identity);

                //Adding Components

                point.gameObject.AddComponent<SphereCollider>(); SphereCollider sc = point.GetComponent<SphereCollider>();

                sc.isTrigger = true;

                point.gameObject.AddComponent<Rigidbody>(); Rigidbody rb = point.GetComponent<Rigidbody>();

                rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ | 
                    RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

                // Determine Destination properties

                float pointSizeY = (activeUnits[c].GetComponent<BoxCollider>().size.x + activeUnits[c].GetComponent<BoxCollider>().size.z) / 2;
                Vector3 pointSize = new Vector3(activeUnits[c].GetComponent<BoxCollider>().size.x, pointSizeY, activeUnits[c].GetComponent<BoxCollider>().size.z);
                point.localScale = pointSize;

                if (activeUnits.Count > 1)
                {
                    //float d = Mathf.Sqrt((pointSize.x * pointSize.z) * 2); // Displacement distance

                    /*
                    float d = 0.7f;

                    float ex = Mathf.Sqrt(activeUnits.Count);
                    xpos++; if (xpos > ex) { xpos = 1; ypos++; }

                    Vector3 startingPosition = new Vector3(centralPosition.x - ((ex * d) / 2), centralPosition.y + 0.25f, centralPosition.z - (ex * d));
                    Vector3 destinationPosition = new Vector3(startingPosition.x + (xpos * d), startingPosition.y, startingPosition.z + (ypos * d));
                    point.transform.position = destinationPosition;
                    */

                    Vector3 destinationPosition = new Vector3((c % 4) + centralPosition.x, centralPosition.y + 0.25f, (c / 4) + centralPosition.z);
                    point.transform.position = destinationPosition;
                }
                else
                {
                    point.transform.position = new Vector3(centralPosition.x, centralPosition.y + 0.25f, centralPosition.z);
                }

                // Polishing

                point.gameObject.AddComponent<Destination>();

                // Assignments

                activeUnits[c].GetComponent<Unit>().destination = point.GetComponent<Destination>(); //activeUnits[c].GetComponent<Unit>().UpdatePath();

                // Debugging

                point.transform.name = "Destination";
                
            }
        }
    }
}