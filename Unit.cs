using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [HideInInspector] public UnityEngine.AI.NavMeshAgent Agent;
    private SelectionManager SelectionManager;

    // [Header("Game-Settings")]

    [Header("Unit-Settings")]
    public UnitType UnitType = new UnitType();
    public string UnitName;
    public bool Selected;

    private void Awake()
    {
        SelectionManager = FindObjectOfType<SelectionManager>();
        if (UnitType == UnitType.Unit || UnitType == UnitType.Worker) Agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    private void Start()
    {
        SelectionManager.Units.Add(this);
    }

    public void Select()
    {
        SelectionManager.ActiveUnits.Add(this);
        Selected = true;
    }

    public void UnSelect()
    {
        SelectionManager.ActiveUnits.Remove(this);
        Selected = false;
    }

    private void OnDestroy()
    {
        if (SelectionManager.ActiveUnits.Contains(this)) SelectionManager.ActiveUnits.Remove(this);
        if (SelectionManager.Units.Contains(this)) SelectionManager.Units.Remove(this);
    }
}

public enum UnitType { Unit, Worker, Building, Resource }