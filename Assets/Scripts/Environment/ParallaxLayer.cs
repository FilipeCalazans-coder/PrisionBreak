using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla o efeito Parallax centralizado.
/// Mantem a peca central alinhada com a camara/jogador e distribui
/// copias para a esquerda e direita, evitando que o fundo suma da tela.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    [Header("Referência da Câmara")]
    [Tooltip("Transform da câmara principal para acompanhar o deslocamento.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Configuração de Movimento")]
    [Tooltip("Fator de velocidade do Parallax: 0 = fixo na câmara, 1 = velocidade do chão.")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxEffect = 0.5f;

    [Header("Configuração de Centralização")]
    [Tooltip("Quantidade total de peças (original + cópias). Use números ímpares (3, 5, 7) para haver um centro exato.")]
    [Min(3)]
    [SerializeField] private int totalTilesCount = 5;

    // Componentes internos
    private SpriteRenderer originalRenderer;
    private float textureWidth;
    private Vector3 lastCameraPosition;

    // Lista de todas as pecas gerenciadas pela esteira
    private List<Transform> activeTiles = new List<Transform>();

    private void Start()
    {
        // 1. Procura a camara caso nao tenha sido associada no Inspector
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            lastCameraPosition = cameraTransform.position;
        }

        // 2. Le a largura exata da imagem visual no mundo
        originalRenderer = GetComponent<SpriteRenderer>();
        textureWidth = originalRenderer.bounds.size.x;

        if (textureWidth <= 0.01f)
        {
            Debug.LogWarning($"[ParallaxLayer] A largura da imagem no objeto '{gameObject.name}' e invalida.");
            return;
        }

        // Garante que a quantidade seja sempre impar para manter um centro exato
        if (totalTilesCount % 2 == 0)
        {
            totalTilesCount++;
        }

        // 3. Monta a esteira centralizada ao redor da camara
        SetupCenteredTiles();
    }

    /// <summary>
    /// Instancia e posiciona as imagens distribuindo metade a esquerda e metade a direita.
    /// </summary>
    private void SetupCenteredTiles()
    {
        // Quantidade de pecas que ficam para cada lado do centro
        int sideOffset = totalTilesCount / 2;

        // Limpa a lista caso seja reiniciada
        activeTiles.Clear();

        // Posiciona a peca original no inicio da fila
        activeTiles.Add(transform);

        // Cria as outras pecas necessarias como clones
        for (int i = 1; i < totalTilesCount; i++)
        {
            GameObject cloneObj = new GameObject($"{gameObject.name}_Clone_{i}");
            cloneObj.transform.SetParent(transform.parent);
            cloneObj.transform.localScale = transform.localScale;

            // Clona todas as propriedades do Sprite original
            SpriteRenderer cloneRenderer = cloneObj.AddComponent<SpriteRenderer>();
            cloneRenderer.sprite = originalRenderer.sprite;
            cloneRenderer.sortingLayerID = originalRenderer.sortingLayerID;
            cloneRenderer.sortingOrder = originalRenderer.sortingOrder;
            cloneRenderer.color = originalRenderer.color;
            cloneRenderer.material = originalRenderer.material;

            activeTiles.Add(cloneObj.transform);
        }

        // Organiza as posicoes de todas as pecas ao redor da camara inicial
        float startCenterX = (cameraTransform != null) ? cameraTransform.position.x : transform.position.x;

        for (int i = 0; i < activeTiles.Count; i++)
        {
            // Indice relativo ao centro: ex: com 5 pecas vai de -2 a +2
            int relativeIndex = i - sideOffset;
            float targetX = startCenterX + (relativeIndex * textureWidth);

            activeTiles[i].position = new Vector3(targetX, transform.position.y, transform.position.z);
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null || textureWidth <= 0.01f) return;

        // 1. Calcula quanto a camara se deslocou desde o ultimo quadro
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;

        // 2. Move todas as imagens juntas na velocidade relativa do Parallax
        Vector3 movementOffset = new Vector3(deltaMovement.x * parallaxEffect, deltaMovement.y * parallaxEffect, 0f);
        for (int i = 0; i < activeTiles.Count; i++)
        {
            if (activeTiles[i] != null)
            {
                activeTiles[i].position += movementOffset;
            }
        }

        // Atualiza a referencia de posicao da camara
        lastCameraPosition = cameraTransform.position;

        // 3. Verifica se a esteira precisa ciclar as pecas das pontas
        KeepTilesCentered();
    }

    /// <summary>
    /// Garante que a peca mais afastada da esteira seja reposicionada para o outro lado
    /// assim que a camara se deslocar, mantendo o jogador sempre no meio.
    /// </summary>
    private void KeepTilesCentered()
    {
        // Identifica qual peca esta mais a esquerda e qual esta mais a direita
        Transform leftmostTile = activeTiles[0];
        Transform rightmostTile = activeTiles[0];

        for (int i = 1; i < activeTiles.Count; i++)
        {
            if (activeTiles[i].position.x < leftmostTile.position.x)
            {
                leftmostTile = activeTiles[i];
            }
            if (activeTiles[i].position.x > rightmostTile.position.x)
            {
                rightmostTile = activeTiles[i];
            }
        }

        // Se a camara andou para a direita e a peca mais a esquerda ficou muito longe para tras:
        // Movemos a peca mais a esquerda para a frente da mais a direita
        float leftDistance = cameraTransform.position.x - leftmostTile.position.x;
        int sideOffset = totalTilesCount / 2;

        if (leftDistance > (sideOffset + 0.5f) * textureWidth)
        {
            leftmostTile.position = new Vector3(rightmostTile.position.x + textureWidth, leftmostTile.position.y, leftmostTile.position.z);
        }
        // Suporte caso a camara recue para a esquerda:
        float rightDistance = rightmostTile.position.x - cameraTransform.position.x;
        if (rightDistance > (sideOffset + 0.5f) * textureWidth)
        {
            rightmostTile.position = new Vector3(leftmostTile.position.x - textureWidth, rightmostTile.position.y, rightmostTile.position.z);
        }
    }
}