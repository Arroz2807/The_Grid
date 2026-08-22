using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Es el IDirectionInputProvider de un enemigo. En cada celda nueva
/// (evento OnCellEntered de LightCycleController): calcula qué
/// direcciones son legales, arma el contexto de decisión, y le pregunta
/// al IEnemyBehavior asignado cuál prefiere.
/// </summary>
[RequireComponent(typeof(LightCycleController))]
public class EnemyBrain : MonoBehaviour, IDirectionInputProvider, ITrailToggleInputProvider
{
    private GridManager gridManager;
    private IEnemyBehavior behavior;
    private LightCycleController controller;
    private LightCycleController target;

    private TurnInput pendingTurn = TurnInput.None;
    private bool pendingToggleRequest;

    private void Awake()
    {
        controller = GetComponent<LightCycleController>();
    }

    /// <summary>
    /// Llamado por GameManager inmediatamente después de instanciar este
    /// enemigo. "initialTarget" es, por ahora, siempre el jugador — el día
    /// que exista selección dinámica de objetivo (modo Todos-contra-todos),
    /// este parámetro es el punto donde se conecta sin tocar el resto de
    /// esta clase.
    /// </summary>
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

        EnemyDecisionContext context = new EnemyDecisionContext(controller, validDirections, target, gridManager);

        // La decisión sobre el rastro se evalúa siempre, incluso si no
        // queda ninguna dirección válida para moverse — son dos preguntas
        // independientes.
        if (behavior.ShouldToggleTrail(context))
        {
            pendingToggleRequest = true;
        }

        if (validDirections.Count == 0)
        {
            // Sin ninguna dirección legal, no hay nada que decidir sobre
            // el giro: el próximo Step() de LightCycleController va a
            // detectar la colisión y morir, con las mismas reglas que el
            // jugador.
            return;
        }

        Vector2Int chosen = validDirections.Count == 1
            ? validDirections[0]
            : behavior.ChooseDirection(context);

        pendingTurn = DirectionToTurn(controller.Direction, chosen);
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

    private static TurnInput DirectionToTurn(Vector2Int from, Vector2Int to)
    {
        if (to == from) return TurnInput.None;
        if (to == GridDirectionUtils.RotateLeft(from)) return TurnInput.Left;
        return TurnInput.Right;
    }

    public TurnInput GetTurnInput()
    {
        TurnInput result = pendingTurn;
        pendingTurn = TurnInput.None;
        return result;
    }

    public bool WasTrailToggleRequested()
    {
        if (!pendingToggleRequest) return false;
        pendingToggleRequest = false;
        return true;
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

        // Línea hacia el objetivo actual — para ver de un vistazo a quién
        // le está apuntando cada enemigo mientras jugás en el Editor.
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.transform.position);
        }
    }
#endif
}