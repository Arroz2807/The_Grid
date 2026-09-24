using UnityEngine;

/// <summary>
/// Contrato de un efecto de power-up. Cada subclase concreta (TurboEffect,
/// ShieldEffect, TrailExtensionEffect) implementa Apply() y Remove(). El
/// sistema de recogida sólo conoce esta clase base — nunca la subclase
/// concreta — lo que permite agregar nuevos efectos sin tocar PowerUpSpawner
/// ni PowerUpPickup.
/// </summary>
public abstract class PowerUpEffect
{
    protected LightCycleController Target { get; }
    protected PowerUpDefinition Definition { get; }
    protected bool IsActive { get; private set; }

    protected PowerUpEffect(LightCycleController target, PowerUpDefinition definition)
    {
        Target = target;
        Definition = definition;
    }

    /// <summary>
    /// Aplica el efecto. Sólo se llama una vez — PowerUpSpawner se encarga
    /// de que no se aplique dos veces al mismo participante.
    /// </summary>
    public void Apply()
    {
        if (IsActive) return;
        IsActive = true;
        OnApply();
    }

    /// <summary>
    /// Elimina el efecto y restaura el estado anterior. Se llama al expirar
    /// la duración, o cuando el participante muere (para evitar estados
    /// colgados). Seguro de llamar aunque el participante ya esté destruido.
    /// </summary>
    public void Remove()
    {
        if (!IsActive) return;
        IsActive = false;
        OnRemove();
    }

    protected abstract void OnApply();
    protected abstract void OnRemove();
}