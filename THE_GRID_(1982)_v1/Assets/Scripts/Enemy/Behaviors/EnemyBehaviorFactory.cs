using UnityEngine;

/// <summary>
/// Traduce un EnemyType a una instancia concreta de IEnemyBehavior. Es el
/// único lugar que necesitó tocarse para que Chase/Predict/Ambush pasaran
/// de "caer a Random" a estar realmente implementados.
/// </summary>
public static class EnemyBehaviorFactory
{
    public static IEnemyBehavior Create(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Random:
                return new RandomBehavior();
            case EnemyType.Chase:
                return new ChaseBehavior();
            case EnemyType.Predict:
                return new PredictBehavior();
            case EnemyType.Ambush:
                return new AmbushBehavior();
            default:
                Debug.LogWarning($"Tipo de enemigo desconocido: {type}. Se usa Random.");
                return new RandomBehavior();
        }
    }
}