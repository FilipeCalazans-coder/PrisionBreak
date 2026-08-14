using UnityEngine;

/// <summary>
/// Garante que todas as moedas, obstáculos e elementos dentro do Chunk 
/// voltem a ficar visíveis e ativos quando o bloco for reaproveitado pelo ObjectPooler.
/// </summary>
public class ChunkResetter : MonoBehaviour
{
    private void OnEnable()
    {
        // Reativa todos os objetos filhos e subfilhos contidos neste Chunk
        ResetAllChildren(transform);
    }

    /// <summary>
    /// Percorre recursivamente a hierarquia de objetos para reativar cada elemento.
    /// </summary>
    /// <param name="parent">Transform do objeto pai a ser verificado.</param>
    private void ResetAllChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // Reativa o objeto filho (ex: Moeda ou Obstáculo)
            child.gameObject.SetActive(true);

            // Se o filho também tiver outros objetos dentro dele, reativa-os também
            if (child.childCount > 0)
            {
                ResetAllChildren(child);
            }
        }
    }
}