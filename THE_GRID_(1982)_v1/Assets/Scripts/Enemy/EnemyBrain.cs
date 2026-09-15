using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Es el IDirectionInputProvider de un enemigo. En cada celda nueva
/// (evento OnCellEntered de LightCycleController): calcula qué
/// direcciones son legales, arma el contexto de decisión, y le pregunta
/// al IEnemyBehavior asignado cuál prefiere.
/// </summary>
[RequireComponent(typeof(LightCycleController))]
public class EnemyBrain : MonoBehaviour, IDirectionInputProvider
{
    private GridManager gridManager;
    private IEnemyBehavior behavior;
    private LightCycleController controller;
    private LightCycleController target;

    private Vector2Int? pendingDirection;

    private void Awake()
    {
        controller = GetComponent<LightCycleController>();
    }

    public void Initialize(GridManager grid, IEnemyBehavior chosenBehavior, LightCycleController initialTarget)
    {
        gridManager = grid;
        behavior = chosenBehavior;
        target = initialTarget;
    }

    private void OnEnable()
    {
        controller.OnCellEntered += HandleCellEntered;
    }

    private void OnDisable()
    {
        controller.OnCellEntered -= HandleCellEntered;
    }

    private void HandleCellEntered()
    {
        List<Vector2Int> validDirections = GetValidDirections();

        if (validDirections.Count == 0)
        {
            return;
        }

        EnemyDecisionContext context = new EnemyDecisionContext(controller, validDirections, target, gridManager);

        pendingDirection = validDirections.Count == 1
            ? validDirections[0]
            : behavior.ChooseDirection(context);
    }

    private List<Vector2Int> GetValidDirections()
    {
        List<Vector2Int> valid = new List<Vector2Int>();

        foreach (Vector2Int candidate in GridDirectionUtils.GetCandidateDirections(controller.Direction))
        {
            Vector2Int nextCell = controller.CurrentCell + candidate;
            if (gridManager.IsInsideGrid(nextCell) && !gridManager.IsCellOccupied(nextCell))
            {
                valid.Add(candidate);
            }
        }

        return valid;
    }

    public Vector2Int? GetDesiredDirection()
    {
        Vector2Int? result = pendingDirection;
        pendingDirection = null;
        return result;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (controller == null) return;

        Gizmos.color = Color.yellow;
        Vector3 from = transform.position;
        Vector3 to = from + new Vector3(controller.Direction.x, controller.Direction.y, 0f) * 0.6f;
        Gizmos.DrawLine(from, to);
        Gizmos.DrawSphere(to, 0.06f);

        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.transform.position);
        }
    }
#endif
}