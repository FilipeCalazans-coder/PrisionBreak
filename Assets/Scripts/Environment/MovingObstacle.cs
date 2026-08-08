using UnityEngine;

/// <summary>
/// Movimenta o obstáculo entre dois pontos usando oscilação matemática (PingPong).
/// </summary>
public class MovingObstacle : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [Tooltip("Distância máxima que o obstáculo percorre no eixo Y ou X.")]
    [SerializeField] private float moveDistance = 3f;

    [Tooltip("Velocidade da movimentação.")]
    [SerializeField] private float speed = 2f;

    [Tooltip("Marque se o movimento for vertical (Cima/Baixo). Desmarque para horizontal (Esquerda/Direita).")]
    [SerializeField] private bool moveVertically = true;

    // Posição inicial do obstáculo no espaço
    private Vector3 startPosition;

    private void Start()
    {
        // Salva a posição inicial onde o obstáculo foi posicionado no Chunk
        startPosition = transform.localPosition;
    }

    private void Update()
    {
        // Calcula a posição oscilante usando Mathf.PingPong
        float pingPongValue = Mathf.PingPong(Time.time * speed, moveDistance);

        if (moveVertically)
        {
            transform.localPosition = startPosition + new Vector3(0f, pingPongValue, 0f);
        }
        else
        {
            transform.localPosition = startPosition + new Vector3(pingPongValue, 0f, 0f);
        }
    }
}