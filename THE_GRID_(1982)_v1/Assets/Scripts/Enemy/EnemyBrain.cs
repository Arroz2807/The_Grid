using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Es el IDirectionInputProvider de un enemigo. En cada celda nueva
/// (evento OnCellEntered de LightCycleController): calcula qué
/// direcciones son legales, consulta opcionalmente a PowerUpSeeker si hay
/// un power-up que valga la pena perseguir, y si no, delega en
/// IEnemyBehavior. EnemyBrain orquesta; no decide la estrategia ni la
/// búsqueda de power-ups — eso es trabajo de sus colaboradores.
/// </summary>
[RequireComponent(typeof(LightCycleController))]
public class EnemyBrain : MonoBehaviour, IDirectionInputProvider
{
    private GridManager gridManager;
    private IEnemyBehavior behavior;
    private LightCycleController controller;
    private LightCycleController target;

    // Opcional: si se asigna, la IA busca power-ups según esta config.
    // Si es null, ignora todos los power-ups — retrocompatible.
    private PowerUpSeeker powerUpSeeker;

    private Vector2Int? pendingDirection;

    private void Awake()
    {
        controller = GetComponent<LightCycleController>();
    }

    /// <summary>
    /// Llamado por GameManager después de instanciar. powerUpSpawner puede
    /// ser null (sin power-ups) o config puede ser null (IA ignora power-ups).
    /// </summary>
    public void Initialize(
        GridManager grid,
        IEnemyBehavior chosenBehavior,
        LightCycleController initialTarget,
        PowerUpSpawner powerUpSpawner = null,
        PowerUpAIConfig powerUpConfig = null)
    {
        gridManager = grid;
        behavior = chosenBehavior;
        target = initialTarget;

        if (powerUpSpawner != null && powerUpConfig != null)
        {
            powerUpSeeker = new PowerUpSeeker(
                gridManager,
                powerUpConfig,
                powerUpSpawner.ActivePickups);
        }
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

        if (validDirections.Count == 0) return;

        Vector2Int chosen;

        // 1. Intentar ir hacia un power-up (sólo si hay un seeker activo).
        if (powerUpSeeker != null)
        {
            Vector2Int? powerUpDir = powerUpSeeker.GetDirectionTowardsBestPickup(
                controller.CurrentCell,
                controller.Direction,
                validDirections);

            if (powerUpDir.HasValue)
            {
                chosen = powerUpDir.Value;
                pendingDirection = chosen;
                return;
            }
        }

        // 2. Sin power-up de interés: comportamiento normal.
        if (validDirections.Count == 1)
        {
            chosen = validDirections[0];
        }
        else
        {
            EnemyDecisionContext context = new EnemyDecisionContext(
                controller, validDirections, target, gridManager);
            chosen = behavior.ChooseDirection(context);
        }

        pendingDirection = chosen;
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